using System;
using Obi;
using UnityEngine;

namespace _Pyrenees.Scripts
{
    

public static class RopeUtils
{
    
    // /// <summary>
    // /// 
    // /// </summary>
    // /// <param name="rope"></param>
    // /// <param name="targetElementIndex"></param>
    // /// <param name="targetPosition"></param>
    // /// <returns>New solver particle index</returns>
    // public static int InsertParticle(ObiRope rope, int targetElementIndex, Vector3 targetPosition)
    // {
    //     var solver = rope.solver;
    //
    //     var targetElement = rope.elements[targetElementIndex];
    //     
    //     int newParticleIndex = AddParticleAt(rope, targetElement.particle1, targetPosition);
    //
    //     if (targetElementIndex == 0)
    //     {
    //         int originalElementEnd = targetElement.particle2;
    //
    //         targetElement.particle2 = newParticleIndex;
    //
    //         rope.elements.Insert(1, new ObiStructuralElement()
    //         {
    //             restLength = Vector3.Distance(solver.positions[originalElementEnd], targetPosition),
    //             particle1 = newParticleIndex,
    //             particle2 = originalElementEnd
    //         });
    //
    //         
    //         var attachments = rope.gameObject.GetComponents<ObiParticleAttachment>();
    //         foreach (var attachment in attachments)
    //         {
    //             Debug.Log($"Attachment {attachment.transform.gameObject.name}: {attachment.particleGroup.particleIndices[0]}");
    //         }
    //         
    //         
    //         
    //         return newParticleIndex;
    //     }
    //     
    //     int originalParticle = targetElement.particle1;
    //     targetElement.particle1 = newParticleIndex;
    //     targetElement.restLength = Vector3.Distance(targetPosition, solver.positions[targetElement.particle2]);
    //     
    //     rope.elements.Insert(targetElementIndex, new ObiStructuralElement()
    //     {
    //         restLength = Vector3.Distance(solver.positions[originalParticle], targetPosition),
    //         particle1 = originalParticle,
    //         particle2 = newParticleIndex
    //     });
    //
    //     return newParticleIndex;
    // }

    public static Vector4 WorldPosition(this ObiSolver solver, int particle)
    {
        return solver.transform.TransformPoint(solver.positions[particle]);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="actorIndex"></param>
    /// <returns>Solver index of added particle</returns>
    public static int AddParticleAt(ObiRopeBase rope, int copyParticleFromIndex, Vector3 position)
    {
        // Copy data from the particle where we will insert new particles, to the particles we will insert:
        int targetIndex = rope.activeParticleCount;
        rope.CopyParticle(rope.solver.particleToActor[copyParticleFromIndex].indexInActor, targetIndex);

        // Move the new particle to the one at the place where we will insert it:
        rope.TeleportParticle(targetIndex, position);

        // Activate the particle:
        rope.ActivateParticle();
        
        return rope.solverIndices[targetIndex];
    }

    public static int InsertParticle(this ObiRopeBase rope, int targetElementIndex, Vector3 insertionPoint)
    {
        if (targetElementIndex == rope.elements.Count - 1)
        {
            InsertElementBefore(rope, targetElementIndex, insertionPoint);
            return rope.elements[targetElementIndex].particle1;
        }
        else
        {
            
            InsertElementAfter(rope, targetElementIndex, insertionPoint);
            return rope.elements[targetElementIndex].particle2;
        }
    }

    public static void InsertElementBefore(this ObiRopeBase rope, int targetElementIndex, Vector3 elementEndPosition, float restLength = -1f)
    {
        var targetElement = rope.elements[targetElementIndex];

        var newParticle = AddParticleAt(rope, targetElement.particle1, elementEndPosition);

        var originalStart = targetElement.particle1;
        targetElement.particle1 = newParticle;
        targetElement.restLength = Vector3.Distance(elementEndPosition, rope.solver.positions[targetElement.particle2]);

        var newElement = new ObiStructuralElement()
        {
            particle1 = originalStart,
            particle2 = newParticle,
            restLength = restLength >= 0 ? restLength : Vector3.Distance(rope.solver.positions[originalStart], elementEndPosition)
        };
        
        rope.elements.Insert(targetElementIndex, newElement);
    }
    
    public static void InsertElementAfter(this ObiRopeBase rope, int targetElementIndex, Vector3 elementEndPosition)
    {
        var targetElement = rope.elements[targetElementIndex];

        var newParticle = AddParticleAt(rope, targetElement.particle2, elementEndPosition);

        var originalEnd = targetElement.particle2;
        targetElement.particle2 = newParticle;
        targetElement.restLength = Vector3.Distance(elementEndPosition, rope.solver.positions[targetElement.particle1]);

        var newElement = new ObiStructuralElement()
        {
            particle1 = newParticle,
            particle2 = originalEnd,
            restLength = Vector3.Distance(rope.solver.positions[originalEnd], elementEndPosition)
        };
        
        rope.elements.Insert(targetElementIndex + 1, newElement);
    }
    
    public static int ShortenRope(ObiRopeBase rope, int targetElement, Vector3 endPosition)
    {
        // Change length of target element to fit new end position
        rope.TeleportParticle(rope.solver.particleToActor[rope.elements[targetElement].particle2].indexInActor, endPosition);
        rope.elements[targetElement].restLength = Vector3.Distance(endPosition, rope.solver.positions[rope.elements[targetElement].particle1]);

        return RemoveElementsAfter(rope, targetElement);
    }

    public static int RemoveElementsAfter(ObiRopeBase rope, int targetElement)
    {
        int removedElements = 0;
        // Already at end of segment
        if (targetElement >= rope.elements.Count - 1 || rope.elements[targetElement].particle2 != rope.elements[targetElement + 1].particle1)
        {
            return removedElements;
        }
        
        int currentElement = targetElement + 1;
        while (true)
        {
            bool lastElement = (currentElement >= rope.elements.Count - 1 || rope.elements[currentElement].particle2 != rope.elements[currentElement + 1].particle1);
            
            removedElements++;
            
            rope.DeactivateParticle(rope.solver.particleToActor[rope.elements[currentElement].particle2].indexInActor);
            rope.elements.RemoveAt(currentElement);

            if (lastElement)
                return removedElements;
        }
        
    }

    public static int GetLastElement(this ObiRopeBase rope)
    {
        for (int i = 0; i < rope.elements.Count; i++)
        {
            if (i == rope.elements.Count - 1 || rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                return i;
        }
        
        Debug.LogError($"Can't find last element of rope with no elements");
        return -1;
    }

    /// <summary>
    /// Only checks first segment. Returns -1 if not found.
    /// </summary>
    public static int GetParticlesFromEnd(this ObiRopeBase rope, int targetParticle)
    {
        int particlesFromEnd = -1;
        for (int i = 0; i < rope.elements.Count; i++)
        {
            if (particlesFromEnd is not -1)
            {
                particlesFromEnd++;
            } 
            // Start counting
            else if (rope.elements[i].particle2 == targetParticle)
            {
                particlesFromEnd = 0;
            }

            if (i < rope.elements.Count - 1 && rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                break;
        }

        if (particlesFromEnd is -1)
        {
            Debug.LogError($"Particle {targetParticle} was not found in first segment");
        }

        return particlesFromEnd;
    }

    /// <summary>
    /// Inclusive of element
    /// </summary>
    public static float GetRestLengthAtElement(this ObiRopeBase rope, int element)
    {
        float restLength = 0;
        for (int i = 0; i < rope.elements.Count; i++)
        {
            restLength += rope.elements[i].restLength;
            if (i == element)
            {
                break;
            }
        }

        return restLength;
    }
    
    public static float GetRestLengthOfFirstSegment(this ObiRopeBase rope)
    {
        if (rope.elements.Count is 0)
            return 0;
        
        float restLength = 0;
        for (int i = 0; i < rope.elements.Count; i++)
        {
            restLength += rope.elements[i].restLength;
            
            // Segment break
            if (i == rope.elements.Count - 1 || rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                break;
        }

        return restLength;
    }

    public static float GetLengthFromEndToElement(this ObiRopeBase rope, int element)
    {
        float restLength = 0;
        
        for (int i = 0; i < rope.elements.Count; i++)
        {
            restLength += rope.elements[i].restLength;
            if (i == element - 1 || i == rope.elements.Count - 1 || rope.elements[i].particle2 != rope.elements[i + 1].particle1)
            {
                break;
            }
        }

        return restLength;
    }
    
    public static Vector3 FindNearestPointToLine(ObiRopeBase rope, Vector3 lineA, Vector3 lineB, out int elementIndex, out float pointDistance)
    {
        var solver = rope.solver;
        pointDistance = float.MaxValue;
        var nearestPoint = Vector3.zero;
        elementIndex = -1;
        
        if (rope.elements.Count is 0)
        {
            Debug.LogError($"Rope has zero elements!");
            return nearestPoint;
        }
        
        // Only care about the first segment
        //
        // if (rope.FirstSegmentElementCount is 1)
        // {
        //     elementIndex = 0;
        //     MathUtils.FindClosestPointsOnLines(lineA, lineB, solver.positions[rope.elements[0].particle1], solver.positions[rope.elements[0].particle2], out var firstPointOnRay, out var firstPointOnRope);
        //     pointDistance = Vector3.Distance(firstPointOnRope, firstPointOnRay);
        //     return solver.positions[rope.elements[0].particle2];
        // }

        for (int i = 1; i < rope.elements.Count; i++)
        {
            var element = rope.elements[i];

            // Check the first element
            MathUtils.FindClosestPointsOnLines(lineA, lineB, solver.positions[element.particle1], solver.positions[element.particle2], out var firstPointOnRay, out var firstPointOnRope);

            var distance = Vector3.Distance(firstPointOnRope, firstPointOnRay);
            if (distance < pointDistance)
            {
                pointDistance = distance;
                nearestPoint = firstPointOnRope;
                elementIndex = i;
            }
            
            // Segment break
            if (i != rope.elements.Count - 1 && rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                break;
        }

        if (elementIndex >= 0)
        {
            return nearestPoint;
        }
        
        throw new Exception($"Couldn't find nearest point on {rope.gameObject.name}. {rope.elements.Count} elements.");
    }

    public static Vector3 FindNearestPointToPoint(ObiRopeBase rope, Vector3 targetPoint, out int elementIndex)
    {
        var nearestPoint = Vector3.zero;
        float pointDistance = float.MaxValue;
        elementIndex = -1;
        
        if (rope.elements.Count is 0)
        {
            Debug.LogError($"Rope has zero elements!");
            return nearestPoint;
        }
        

        // if (rope.FirstSegmentElementCount is 1)
        // {
        //     elementIndex = 0;
        //     return rope.solver.positions[rope.elements[0].particle2];
        // }
        
        // Skip the first element!
        // TODO
        for (int i = 1; i < rope.elements.Count; i++)
        {
            var projectedPoint = MathUtils.ProjectPointOnLine(rope.solver.positions[rope.elements[i].particle1], rope.solver.positions[rope.elements[i].particle2], targetPoint);
            
            float distance = Vector3.Distance(targetPoint, projectedPoint);
            if (distance < pointDistance)
            {
                elementIndex = i;
                nearestPoint = projectedPoint;
                pointDistance = distance;
            }
            
            // Segment break
            if (i != rope.elements.Count - 1 && rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                break;
        }

        return nearestPoint;
    }
    
    // public static int GetNearestParticle(ObiSolver solver, float sphereCastRadius, Vector3 position)
    // {
    //     int filter = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 0);
    //     var query = new QueryShape(QueryShape.QueryType.Sphere, Vector3.zero, Vector3.zero, 0, sphereCastRadius, filter);
    //     var xform = new AffineTransform(position, Quaternion.identity, Vector4.one);
    //
    //     QueryResult[] results = solver.EnqueueSpatialQuery(query, xform);
    //
    //     int closestParticle = -1;
    //     float shortestDistance = float.MaxValue;
    //     // Iterate over results and draw their distance to the point.
    //     // We're assuming the solver only contains 0-simplices (particles).
    //     for (int i = 0; i < results.Length; ++i)
    //     {
    //         if (results[i].distance <= sphereCastRadius && results[i].distance < shortestDistance)
    //         {
    //             closestParticle = results[i].simplexIndex;
    //             shortestDistance = results[i].distance;
    //         }
    //     }
    //
    //     return closestParticle;
    // }

    public static float GetRestLengthAtParticle(this ObiRopeBase rope, int particle)
    {
        float length = 0f;

        if (rope.elements[0].particle1 == particle)
            return length;
        
        for (int i = 0; i < rope.elements.Count; i++)
        {
            length += rope.elements[i].restLength;

            if (rope.elements[i].particle2 == particle)
                return length;
            
            // Segment break
            if (i != rope.elements.Count - 1 && rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                break;
        }
        
        Debug.LogError($"Particle {particle} was not found in {rope.gameObject.name}");
        return 0f;
    }
    
    public static float GetTotalStretchShearForce(ObiRopeBase rope, float squareSubstepTime)
    {
        var dc = rope.GetConstraintsByType(Oni.ConstraintType.StretchShear) as ObiConstraints<ObiStretchShearConstraintsBatch>;
        var sc = rope.solver.GetConstraintsByType(Oni.ConstraintType.StretchShear) as ObiConstraints<ObiStretchShearConstraintsBatch>;

        if (dc is null || sc is null)
        {
            Debug.LogError($"Couldn't get StretchShear constraints");
            return 0f;
        }

        float totalForce = 0f;

        for (int j = 0; j < dc.batchCount; ++j)
        {
            var batch = dc.GetBatch(j) as ObiStretchShearConstraintsBatch;
            var solverBatch = sc.batches[j] as ObiStretchShearConstraintsBatch;

            for (int i = 0; i < batch.activeConstraintCount; i++)
            {
                int elementIndex = j + 2 * i;

                // divide lambda by squared delta time to get force in newtons:
                int offset = rope.solverBatchOffsets[(int)Oni.ConstraintType.StretchShear][j];
                float force = solverBatch.lambdas[offset + i] / squareSubstepTime;

                totalForce += Mathf.Abs(force);
            }
        }

        return totalForce;
    }

    
    public static float GetTotalBendTwistForce(ObiRopeBase rope, float squareSubstepTime)
    {
        var dc = rope.GetConstraintsByType(Oni.ConstraintType.BendTwist) as ObiConstraints<ObiBendTwistConstraintsBatch>;
        var sc = rope.solver.GetConstraintsByType(Oni.ConstraintType.BendTwist) as ObiConstraints<ObiBendTwistConstraintsBatch>;

        if (dc is null || sc is null)
        {
            Debug.LogError($"Couldn't get BendTwist constraints");
            return 0f;
        }

        float totalForce = 0f;

        for (int j = 0; j < dc.batchCount; ++j)
        {
            var batch = dc.GetBatch(j) as ObiBendTwistConstraintsBatch;
            var solverBatch = sc.batches[j] as ObiBendTwistConstraintsBatch;

            for (int i = 0; i < batch.activeConstraintCount; i++)
            {
                int elementIndex = j + 2 * i;

                // divide lambda by squared delta time to get force in newtons:
                int offset = rope.solverBatchOffsets[(int)Oni.ConstraintType.BendTwist][j];
                float force = solverBatch.lambdas[offset + i] / squareSubstepTime;

                totalForce += Mathf.Abs(force);
            }
        }

        return totalForce;
    }

    // public static int GetElementCountOfSegment(this ObiRopeBase rope, int startingElement)
    // {
    //     for (int i = 0; i < rope.elements.Count; i++)
    //     {
    //         // Segment break
    //         if (i == rope.elements.Count - 1 || rope.elements[i].particle2 != rope.elements[i + 1].particle1)
    //             return i + 1;
    //     }
    //
    //     return 0;
    // }
    
    public static bool DoesSegmentHaveElementCount(this ObiRopeBase rope, int startingElement, int elementCount)
    {
        for (int i = 0; i < rope.elements.Count; i++)
        {
            if (i + 1 >= elementCount)
                return true;
            
            // Segment break
            if (i == rope.elements.Count - 1 || rope.elements[i].particle2 != rope.elements[i + 1].particle1)
                return false;
        }

        return false;
    }

    public static void RecalculateState(this ObiRopeBase rope)
    {
        // recalculate rest positions and length prior to constraints (bend constraints need rest positions):
        rope.RecalculateRestPositions();
        rope.RecalculateRestLength();
        // rope.RecalculateFirstSegmentElementCount();
    
        // rebuild constraints:
        rope.RebuildConstraintsFromElements();
    }
}
}

