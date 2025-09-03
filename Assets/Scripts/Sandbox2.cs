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
    private int _preEyeletElement = -1;

    private float _preEyeletElementLengthDelta;

    private void OnEnable()
    {
        rope.OnSubstepsStart += RopeOnOnSubstepsStart;
        rope.OnSimulationStart += RopeOnOnSimulationStart;
        
    }
    
    private void OnDisable()
    {
        rope.OnSubstepsStart -= RopeOnOnSubstepsStart;
        rope.OnSimulationStart -= RopeOnOnSimulationStart;
    }

    private IEnumerator Start()
    {
        while (true)
        {
            if (rope.isLoaded || Time.time > 3f)
            {
                CreateEyeletElement();
                yield break;
            }
            yield return null;
        }
    }
    
    private void RopeOnOnSimulationStart(ObiActor actor, float simulatedTime, float substepTime)
    {
        
        LockEyeletPosition();
        
        UpdateEyeletConstraint();
    }
    
    private void RopeOnOnSubstepsStart(ObiActor actor, float simulatedTime, float substepTime)
    {
        
    }

    private void UpdateEyeletConstraint()
    {
        if (_preEyeletElement is -1)
            return;

        var preEyeletDelta = Vector3.Distance(Solver.positions[rope.elements[_preEyeletElement].particle1], Solver.positions[rope.elements[_preEyeletElement].particle2]);
        _preEyeletElementLengthDelta = preEyeletDelta - rope.elements[_preEyeletElement].restLength;

        rope.elements[_preEyeletElement].restLength = Mathf.Max(0f,rope.elements[_preEyeletElement].restLength + Mathf.Min(_preEyeletElementLengthDelta * speediness, rope.elements[_preEyeletElement + 1].restLength));
        rope.elements[_preEyeletElement + 1].restLength = Mathf.Max(0f, rope.elements[_preEyeletElement + 1].restLength - Mathf.Min(_preEyeletElementLengthDelta * speediness, rope.elements[_preEyeletElement].restLength));
        
        rope.RecalculateRestLength();
        rope.RecalculateRestPositions();
        rope.RebuildConstraintsFromElements();
    }

    private void LockEyeletPosition()
    {
        if (_preEyeletElement is -1)
            return;

        var eyeletParticle = rope.elements[_preEyeletElement].particle2;

        Solver.invMasses[eyeletParticle] = 0f;
        Solver.velocities[eyeletParticle] = Vector3.zero;
        Solver.startPositions[eyeletParticle] = transform.position;
        Solver.endPositions[eyeletParticle] = transform.position;
        Solver.positions[eyeletParticle] = transform.position;
    }

    private void CreateEyeletElement()
    {
        if (startElement is -1 || startElement >= rope.elements.Count)
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

        SetPreEyeletElement(startElement);
    }

    private void SetPreEyeletElement(int element)
    {
        // Reset previous eyelet particle
        if (_preEyeletElement is not -1)
            rope.CopyParticle(rope.elements[_preEyeletElement].particle1, rope.elements[_preEyeletElement].particle2);
        
        // Set eyelet particle to be static
        Solver.invMasses[rope.elements[element].particle2] = 0;
        
        rope.UpdateParticleProperties();
        _preEyeletElement = element;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        if (!Application.isPlaying)
            return;
    }
    
    private void OnGUI()
    {

        
        string labelText = $"PreEyelet Length: {_preEyeletElementLengthDelta.Round(4)}";
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 45 // optional, make text bigger
        };

        // Measure the size of the label
        Vector2 size = style.CalcSize(new GUIContent(labelText));
        size.y *= 3f;

        // Position it at bottom center
        float x = (Screen.width - size.x) / 5f;
        float y = Screen.height - size.y - 10f; // 10px margin from bottom

        GUI.Label(new Rect(x, y, size.x, size.y), labelText, style);
    }
}
