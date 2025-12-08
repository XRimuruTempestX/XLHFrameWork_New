using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLHFramework.GCFrameWork.Base;

namespace XLHFramework.GCFrameWork.Runtime
{
	public class SoldierWorldScriptExecutionOrder : IBehaviourExecution
	{
		public static string worldName = "SoldierWorld";

		private static readonly string[] LogicBehaviorExecutions = new string[] {};

		private static readonly string[] DataBehaviorExecutions = new string[] {};

		private static readonly string[] MsgBehaviorExecutions = new string[] {};

		public string[] GetDataBehaviourExecution(){ return DataBehaviorExecutions; }

		public string[] GetLogicBehaviourExecution(){ return LogicBehaviorExecutions; }

		public string[] GetMsgBehaviourExecution(){ return MsgBehaviorExecutions; }
	}
}
