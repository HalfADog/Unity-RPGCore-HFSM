using System;
using UnityEngine;

namespace RPGCore.AI.HFSM
{
    /// <summary>
    /// ×´Ì¬»úControllerÅäÖÃÊı¾İ
    /// </summary>
    [System.Serializable]
	public class StateMachineControllerConfig
    {
        public bool CustomFilePath;
        public string FilePath;
        public bool UseNamespace;
        public string Namespace;
        public bool DisperseGenerate;
        public bool DisperseAll;
    }
}