using System;
using _Pyrenees.Scripts;
using Obi;
using UnityEngine;

public class Sandbox2 : MonoBehaviour
{
    public ObiRope rope;
    public int startElement;

    private void OnEnable()
    {
        rope.OnBlueprintLoaded += RopeOnOnBlueprintLoaded;
    }

    private void OnDisable()
    {
        rope.OnBlueprintLoaded -= RopeOnOnBlueprintLoaded;

    }

    private void RopeOnOnBlueprintLoaded(ObiActor actor, ObiActorBlueprint blueprint)
    {
        // rope.CopyParticle(0, rope.activeParticleCount);
        rope.DeactivateParticle(0);
        
        // rope.indi
        var element = new ObiStructuralElement()
        {
            restLength = 0f, particle1 = rope.elements[^1].particle2, particle2 = rope.solverIndices[rope.activeParticleCount - 1]
        };

        rope.elements.Add(element);

        rope.RebuildConstraintsFromElements();
        
        for (int i = 0; i < rope.solverIndices.count; i++)
        {
            print($"{i} is {rope.IsParticleActive(i)}");
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        if (!Application.isPlaying)
            return;
        
        
    }
}
