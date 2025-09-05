using System.Collections;
using Obi;
using Sirenix.OdinInspector;
using UnityEngine;

public class Sandbox2 : MonoBehaviour
{
    public ObiRope rope;
    public ObiRod rod;
    public int startElement = -1;

    [Range(0, 11)] public float speediness = 1f;
    // public int actorStaticParticle;

    private ObiSolver Solver => rope.solver;

    /// <summary>
    /// The element leading into the eyelet; i.e., its second particle will be the actual eyelet
    /// </summary>
    [ShowInInspector, ReadOnly] private int _leadInElementIndex = -1;

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

    private void RopeOnOnSubstepsStart(ObiActor actor, float simulatedTime, float substepTime)
    {
    }

    private void RopeOnOnSimulationStart(ObiActor actor, float simulatedTime, float substepTime)
    {
        // LockEyeletPosition();
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
        int newEyeletParticle = rope.elements[element].particle2;
        // Reset previous eyelet particle
        if (_leadInElementIndex is not -1)
        {
            int sourceParticle = element > _leadInElementIndex ? rope.elements[_leadInElementIndex].particle1 : rope.elements[_leadInElementIndex + 1].particle2;
            int oldEyeletParticle = rope.elements[_leadInElementIndex].particle2;
            
            var oldEyeletProperties = GetParticleSnapshot(Solver, oldEyeletParticle);
            var sourceParticleProperties = GetParticleSnapshot(Solver, sourceParticle);
            
            ApplyParticleSnapshot(Solver, oldEyeletParticle, sourceParticleProperties);
            ApplyParticleSnapshot(Solver, newEyeletParticle, oldEyeletProperties);
            // TODO: IDK why this is getting changed when copying properties
            Solver.invMasses[newEyeletParticle] = 10f;
            Solver.invMasses[oldEyeletParticle] = 10f;
        }
        else
        {
            Solver.velocities[newEyeletParticle] = Vector4.zero;
            Solver.angularVelocities[newEyeletParticle] = Vector4.zero;
        }
        
        rope.UpdateParticleProperties();
        _leadInElementIndex = element;
        print($"Lead In Element: {element}");
        
        Stitch(GetActorIndex(rope, rope.elements[_leadInElementIndex].particle2), 5);
    }

    private ObiStitcher _stitcher;

    [Button]
    private void Stitch(int ropeLocalIndex, int rodLocalIndex)
    {
        if (_stitcher == null)
        {
            _stitcher = rod.gameObject.AddComponent<ObiStitcher>();
            _stitcher.Actor1 = rod;
            _stitcher.Actor2 = rope;
        }
        else
        {
            _stitcher.Clear();
        }

        _stitcher.AddStitch(rodLocalIndex, ropeLocalIndex);
        print($"Stitched: {ropeLocalIndex} <---> {rodLocalIndex}");
        _stitcher.PushDataToSolver();
    }

    private static int LocalRopeIndexToSolver(ObiRopeBase rope, int localIndex)
    {
        if (localIndex < 0 || localIndex > rope.elements.Count)
        {
            Debug.LogError($"Local rope index {localIndex} out of range");
            return -1;
        }

        return localIndex % 2 is 0 ? rope.elements[localIndex].particle1 : rope.elements[localIndex - 1].particle2;
    }

    private static int GetActorIndex(ObiRopeBase actor, int solverIndex)
    {
        for (int i = 0; i < actor.activeParticleCount; i++)
        {
            if (actor.solverIndices[i] == solverIndex)
                return i;
        }

        Debug.LogError($"Actor index for {solverIndex} not found");
        return -1;
    }

    private static ParticleSnapshot GetParticleSnapshot(ObiSolver solver, int solverIndex)
    {
        var snapshot = new ParticleSnapshot();

        // positions
        snapshot.Position = solver.positions[solverIndex];
        snapshot.PrevPosition = solver.prevPositions[solverIndex];
        snapshot.RestPosition = solver.restPositions[solverIndex];
        snapshot.StartPosition = solver.startPositions[solverIndex];
        snapshot.EndPosition = solver.endPositions[solverIndex];
        snapshot.RenderablePosition = solver.renderablePositions[solverIndex];

        // orientations
        snapshot.Orientation = solver.orientations[solverIndex];
        snapshot.PrevOrientation = solver.prevOrientations[solverIndex];
        snapshot.RestOrientation = solver.restOrientations[solverIndex];
        snapshot.StartOrientation = solver.startOrientations[solverIndex];
        snapshot.EndOrientation = solver.endOrientations[solverIndex];
        snapshot.RenderableOrientation = solver.renderableOrientations[solverIndex];

        // velocities
        snapshot.Velocity = solver.velocities[solverIndex];
        snapshot.AngularVelocity = solver.angularVelocities[solverIndex];

        // masses
        snapshot.InvMass = solver.invMasses[solverIndex];
        snapshot.InvRotationalMass = solver.invRotationalMasses[solverIndex];

        return snapshot;
    }

    public static void ApplyParticleSnapshot(ObiSolver solver, int solverIndex, ParticleSnapshot snapshot)
    {
        // positions
        // solver.positions[solverIndex] = snapshot.Position;
        // solver.prevPositions[solverIndex] = snapshot.PrevPosition;
        // solver.restPositions[solverIndex] = snapshot.RestPosition;
        // solver.startPositions[solverIndex] = snapshot.StartPosition;
        // solver.endPositions[solverIndex] = snapshot.EndPosition;
        // solver.renderablePositions[solverIndex] = snapshot.RenderablePosition;

        // orientations
        solver.orientations[solverIndex] = snapshot.Orientation;
        solver.prevOrientations[solverIndex] = snapshot.PrevOrientation;
        solver.restOrientations[solverIndex] = snapshot.RestOrientation;
        solver.startOrientations[solverIndex] = snapshot.StartOrientation;
        solver.endOrientations[solverIndex] = snapshot.EndOrientation;
        solver.renderableOrientations[solverIndex] = snapshot.RenderableOrientation;

        // velocities
        solver.velocities[solverIndex] = snapshot.Velocity;
        solver.angularVelocities[solverIndex] = snapshot.AngularVelocity;

        // masses
        solver.invMasses[solverIndex] = snapshot.InvMass;
        solver.invRotationalMasses[solverIndex] = snapshot.InvRotationalMass;
    }

    public struct ParticleSnapshot
    {
        // positions
        public Vector4 Position;
        public Vector4 PrevPosition;
        public Vector4 RestPosition;

        public Vector4 StartPosition;
        public Vector4 EndPosition;
        public Vector4 RenderablePosition;

        // orientations
        public Quaternion Orientation;
        public Quaternion PrevOrientation;
        public Quaternion RestOrientation;

        public Quaternion StartOrientation;
        public Quaternion EndOrientation;
        public Quaternion RenderableOrientation;

        // velocities
        public Vector4 Velocity;
        public Vector4 AngularVelocity;

        // masses tensors
        public float InvMass;
        public float InvRotationalMass;
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