using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity;
using UnityEditor;

[CustomEditor(typeof(SetBoidTarget))]
public class SetTargetEditor : Editor
{
    private SetBoidTarget _target;

    public override void OnInspectorGUI()
    {
        _target = (SetBoidTarget)target;
        base.OnInspectorGUI();
        GUILayout.Space(20);
        if(GUILayout.Button("Attack"))
        {
            _target.Attack();
        }
    }
}
