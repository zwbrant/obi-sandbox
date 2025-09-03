using System;
using System.Collections;
using _Pyrenees.Scripts;
using Obi;
using Sirenix.OdinInspector;
using UnityEngine;

public class Sandbox2 : MonoBehaviour
{
    public ObiRope rope;
    public int eyeletParticle;
    public int startElement;

    // private void OnEnable()
    // {
    //     rope.OnBlueprintLoaded += RopeOnOnBlueprintLoaded;
    //     rope.OnSubstepsStart += RopeOnOnSubstepsStart;
    // }
    //
    // private void OnDisable()
    // {
    //     rope.OnBlueprintLoaded -= RopeOnOnBlueprintLoaded;
    //     rope.OnSubstepsStart -= RopeOnOnSubstepsStart;
    // }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(2f);
        DoThing();
    }

    private void RopeOnOnSubstepsStart(ObiActor actor, float simulatedTime, float substepTime)
    {
        
    }

    private void RopeOnOnBlueprintLoaded(ObiActor actor, ObiActorBlueprint blueprint)
    {
        print(rope.activeParticleCount);

        // rope.InsertParticle(rope.elements.Count - 1, (Vector3)rope.solver.positions[rope.elements[^1].particle2] + Vector3.right * .5f);
        
        // Duplicate particle traits to new one
        rope.CopyParticle(rope.activeParticleCount - 1, rope.activeParticleCount);
        
        // Move it to position
        rope.TeleportParticle(rope.activeParticleCount, transform.position);
       
        // rope.ActivateParticle();
        // rope.TeleportParticle(rope.activeParticleCount - 1, (Vector3)rope.solver.positions[rope.elements[^1].particle2] + Vector3.right * .5f);
        var element = new ObiStructuralElement()
        {
            particle1 = rope.elements[^1].particle2,
            particle2 = rope.solverIndices[rope.activeParticleCount], 
            restLength = 2f
        };
        
        rope.elements.Add(element);
        
        // Activate the particle:
        rope.ActivateParticle();
        
        rope.SetRenderingDirty(Oni.RenderingSystemType.AllRopes);
        
        rope.RebuildConstraintsFromElements();
        print(rope.activeParticleCount);
    }
    
    private int AddParticleAt(int index)
    {
        int targetIndex = rope.activeParticleCount;

        // Copy data from the particle where we will insert new particles, to the particles we will insert:
        rope.CopyParticle(0, targetIndex);

        // Move the new particle to the one at the place where we will insert it:
        rope.TeleportParticle(targetIndex, rope.solver.positions[rope.solverIndices[index]]);

        // Activate the particle:
        rope.ActivateParticle();
        rope.SetRenderingDirty(Oni.RenderingSystemType.AllRopes);

        return rope.solverIndices[targetIndex];
    }

    [Button]
    private void DoThing()
    {
        RopeOnOnBlueprintLoaded(null, null);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        if (!Application.isPlaying)
            return;
    }
}
