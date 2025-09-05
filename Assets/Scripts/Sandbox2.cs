using System.Collections;
using Obi;
using Sirenix.OdinInspector;
using UnityEngine;

public class Sandbox2 : MonoBehaviour
{
    public ObiRope rope;
    public int startElement = -1;

    [Range(0,11)] public float speediness = 1f;
    // public int actorStaticParticle;
    
    private ObiSolver Solver => rope.solver;

    /// <summary>
    /// The element leading into the eyelet; i.e., its second particle will be the actual eyelet
    /// </summary>
    private int _leadInElementIndex = -1;

    private float _leadInElementStretch, _leadOutElementStretch;

    private void OnEnable()
    {
        rope.OnElementsGenerated += RopeOnOnElementsGenerated;
        rope.OnSubstepsStart += RopeOnOnSubstepsStart;
        rope.OnSimulationStart += RopeOnOnSimulationStart;
        rope.OnSubstepsStart += RopeOnOnSubstepsStart;
    }

    private void RopeOnOnElementsGenerated(ObiActor actor)
    {
        CreateEyeletElement();
    }


    private void OnDisable()
    {
        rope.OnElementsGenerated -= RopeOnOnElementsGenerated;
        rope.OnSubstepsStart -= RopeOnOnSubstepsStart;
        rope.OnSimulationStart -= RopeOnOnSimulationStart;
        rope.OnSubstepsStart -= RopeOnOnSubstepsStart;
    }

    // private IEnumerator Start()
    // {
    //     while (true)
    //     {
    //         if (rope.isLoaded || Time.time > 3f)
    //         {
    //             CreateEyeletElement();
    //             yield break;
    //         }
    //         yield return null;
    //     }
    // }
    
    private void RopeOnOnSubstepsStart(ObiActor actor, float simulatedTime, float substepTime)
    {

    }
    
    private void RopeOnOnSimulationStart(ObiActor actor, float simulatedTime, float substepTime)
    {
        LockEyeletPosition();
        UpdateEyeletConstraint();
    }

    private void UpdateEyeletConstraint()
    {
        if (_leadInElementIndex is -1)
            return;
        
        var leadInElement = rope.elements[_leadInElementIndex];
        var leadOutElement = rope.elements[_leadInElementIndex + 1];

        _leadInElementStretch = Vector3.Distance(Solver.positions[leadInElement.particle1], Solver.positions[leadInElement.particle2]);
        _leadInElementStretch -= leadInElement.restLength;
        
        _leadOutElementStretch = Vector3.Distance(Solver.positions[leadOutElement.particle1], Solver.positions[leadOutElement.particle2]);
        _leadOutElementStretch -= leadOutElement.restLength;
        
        var stretchDelta = _leadInElementStretch - _leadOutElementStretch;

        leadInElement.restLength = Mathf.Max(0f, leadInElement.restLength + Mathf.Min(stretchDelta * speediness, leadOutElement.restLength));
        leadOutElement.restLength = Mathf.Max(0f, leadOutElement.restLength - Mathf.Max(stretchDelta * speediness, -leadInElement.restLength));

        if (leadOutElement.restLength < MathUtils.ScalarEpsilon && _leadInElementIndex < rope.elements.Count - 2)
            SetLeadInElement(_leadInElementIndex + 1);
        else if (leadInElement.restLength < MathUtils.ScalarEpsilon && _leadInElementIndex > 0)
            SetLeadInElement(_leadInElementIndex - 1);
        
        rope.RecalculateRestLength();
        rope.RecalculateRestPositions();
        rope.RebuildConstraintsFromElements();
    }

    private void LockEyeletPosition()
    {
        if (_leadInElementIndex is -1)
            return;

        var eyeletParticle = rope.elements[_leadInElementIndex].particle2;

        Solver.invMasses[eyeletParticle] = 0f;
        Solver.velocities[eyeletParticle] = Vector3.zero;
        Solver.startPositions[eyeletParticle] = transform.position;
        Solver.endPositions[eyeletParticle] = transform.position;
        Solver.positions[eyeletParticle] = transform.position;
    }

    private void CreateEyeletElement()
    {
        if (startElement is -1 || startElement >= rope.elements.Count - 1)
            return;
        
        // Duplicate particle traits to new one
        rope.CopyParticle(rope.activeParticleCount - 1, rope.activeParticleCount);
        
        // Move it to position
        rope.TeleportParticle(rope.activeParticleCount, ((Vector3)rope.solver.positions[rope.elements[startElement].particle1] + (Vector3)rope.solver.positions[rope.elements[startElement].particle2]) * .5f);

        var element = new ObiStructuralElement()
        {
            particle1 = rope.elements[startElement].particle1,
            particle2 = rope.solverIndices[rope.activeParticleCount], 
            restLength = rope.elements[0].restLength * .5f
        };

        rope.elements[startElement].particle1 = element.particle2;
        rope.elements[startElement].restLength *= .5f;
        
        rope.elements.Insert(startElement, element);
        
        // Activate the particle:
        rope.ActivateParticle();
        
        rope.SetRenderingDirty(Oni.RenderingSystemType.AllRopes);
        
        rope.RecalculateRestLength();
        rope.RebuildConstraintsFromElements();

        SetLeadInElement(startElement);
    }

    private void SetLeadInElement(int element)
    {
        // Reset previous eyelet particle
        if (_leadInElementIndex is not -1)
        {
            int sourceParticle = element > _leadInElementIndex ? rope.elements[_leadInElementIndex].particle1 : rope.elements[_leadInElementIndex + 1].particle2;
            int oldEyeletParticle = rope.elements[_leadInElementIndex].particle2;
            
            // Copy solver data:
            // Solver.prevPositions[oldEyeletParticle] = Solver.prevPositions[sourceParticle];
            // Solver.restPositions[oldEyeletParticle] = Solver.restPositions[sourceParticle];
            // Solver.endPositions[oldEyeletParticle] = Solver.endPositions[sourceParticle];
            // Solver.startPositions[oldEyeletParticle] =  Solver.startPositions[sourceParticle];
            // Solver.positions[oldEyeletParticle] = Solver.positions[sourceParticle];

            Solver.prevOrientations[oldEyeletParticle] = Solver.prevOrientations[sourceParticle];
            Solver.restOrientations[oldEyeletParticle] = Solver.restOrientations[sourceParticle];
            Solver.endOrientations[oldEyeletParticle] = Solver.startOrientations[oldEyeletParticle] = Solver.orientations[oldEyeletParticle] = Solver.orientations[sourceParticle];

            Solver.velocities[oldEyeletParticle] = Solver.velocities[sourceParticle];
            Solver.angularVelocities[oldEyeletParticle] = Solver.angularVelocities[sourceParticle];
            Solver.invMasses[oldEyeletParticle] = Solver.invMasses[sourceParticle];
            Solver.invRotationalMasses[oldEyeletParticle] = Solver.invRotationalMasses[sourceParticle];
            Solver.principalRadii[oldEyeletParticle] = Solver.principalRadii[sourceParticle];
            Solver.phases[oldEyeletParticle] = Solver.phases[sourceParticle];
            Solver.filters[oldEyeletParticle] = Solver.filters[sourceParticle];
            Solver.colors[oldEyeletParticle] = Solver.colors[sourceParticle];
        }
        
        // Set eyelet particle to be static
        Solver.invMasses[rope.elements[element].particle2] = 0;
        Solver.velocities[rope.elements[element].particle2] = Vector4.zero;
        
        rope.UpdateParticleProperties();
        _leadInElementIndex = element;
        // print($"Lead In Element: {element}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.11f);
        
        if (!Application.isPlaying)
            return;
    }
    
    private void OnGUI()
    {
        string labelText = $"Lead-in Element Stretch: {_leadInElementStretch.Round(4)}";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 45 // optional, make text bigger
        };

        // Measure the size of the label
        Vector2 size = style.CalcSize(new GUIContent(labelText));
        size.y *= 3f;

        float x = (Screen.width - size.x) / 5f;
        float y = Screen.height - size.y - 10f; // 10px margin from bottom

        GUI.Label(new Rect(x, y, size.x, size.y), labelText, style);
        
        labelText = $"Lead-out Element Stretch: {_leadOutElementStretch.Round(4)}";
        x = (Screen.width - size.x) * .8f;
        
        
        GUI.Label(new Rect(x, y, size.x, size.y), labelText, style);
    }
}
