//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.Core.FSM
{
	public interface IBlackboard
	{
		bool GetBoolValue(string name);
		float GetFloatValue(string name);
		int GetIntValue(string name);
		bool GetTriggerState(string name);
		void SetBool(string name, bool value);
		void SetFloat(string name, float value);
		void SetInt(string name, int value);
		void SetTrigger(string name);

		void Reset();
		void ResetTriggers();
	}
}