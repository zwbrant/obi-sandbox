using System;
using System.Collections.Generic;
using Obi;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Sandbox1 : MonoBehaviour
{
    public GameObject canvasPrefab;
    public GameObject attachmentPrefab;
    public ObiRope rope;
    public ObiRopeCursor ropeCursor;
    [Range(-1, 1)] public float changeLength;

    private List<TMP_Text> _texts;
    private Transform _textsParent;
    private TMP_Text _actorText;

    private void OnEnable()
    {
        rope.OnSimulationStart += RopeOnOnSimulationStart;
    }
    private void OnDisable()
    {
        rope.OnSimulationStart -= RopeOnOnSimulationStart;
    }
    
    private void Start()
    {
        _texts = new List<TMP_Text>();

        _textsParent = new GameObject($"{gameObject.name}_Texts").transform;
        
        // Assume actor is initialized
        for (int i = 0; i < rope.particleCount; i++)
        {
            var text = Instantiate(canvasPrefab, _textsParent.transform, false).GetComponentInChildren<TMP_Text>();
            _texts.Add(text);
        }
        
        _actorText = Instantiate(canvasPrefab, _textsParent.transform, false).GetComponentInChildren<TMP_Text>();
        _actorText.transform.parent.gameObject.name = $"{gameObject.name}_ActorText";
        _actorText.color = Color.darkOrange;
    }

    private void Update()
    {
        if (Mathf.Abs(changeLength) < .001f)
            return;
        
        ropeCursor.ChangeLength(Time.deltaTime * changeLength * 3f);
    }

    private void RopeOnOnSimulationStart(ObiActor actor, float simulatedTime, float substepTime)
    {
        _actorText.text = $"{rope.activeParticleCount}/{rope.particleCount}";
        _actorText.transform.parent.position = rope.transform.position + Vector3.up * .5f;
        
        while (_texts.Count != actor.activeParticleCount)
        {
            if (_texts.Count > actor.activeParticleCount)
            {
                int lastIndex = _texts.Count - 1;
                Destroy(_texts[lastIndex].transform.parent.gameObject);
                _texts.RemoveAt(_texts.Count - 1);
            }
            else 
                _texts.Add(Instantiate(canvasPrefab, _textsParent.transform, false).GetComponentInChildren<TMP_Text>());
        }

        for (var i = 0; i < actor.activeParticleCount; i++)
        {
            _texts[i].transform.parent.position = (Vector3)actor.solver.positions[actor.solverIndices[i]] + Vector3.back * .2f;
            _texts[i].text = $"Ai: {i}\nSi: {actor.solverIndices[i]}";
        }
    }

    [Button]
    public void AttachAtParticle(int actorParticle)
    {
        var attachment = Instantiate(attachmentPrefab);
        
        
    }
    
}
