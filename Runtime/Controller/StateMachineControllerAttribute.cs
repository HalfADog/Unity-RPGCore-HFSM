using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace RPGCore.AI.HFSM
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple = false,Inherited =false)]
    public class StateMachineControllerAttribute : Attribute
    {
        public string ControllerName;
    }
}
