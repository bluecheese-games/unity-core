//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System.Collections.Generic;

namespace BlueCheese.Core.FSM
{
	public class Blackboard : IBlackboard
	{
		private readonly Dictionary<string, bool> _boolParameters = new();
		private readonly Dictionary<string, int> _intParameters = new();
		private readonly Dictionary<string, float> _floatParameters = new();
		private readonly HashSet<string> _triggers = new();

		/// <summary>
		/// Gets the trigger state with the specified name.
		/// </summary>
		/// <param name="name">The name of the trigger state.</param>
		/// <returns>True if the trigger state is active, false otherwise.</returns>
		public bool GetTriggerState(string name) => _triggers.Contains(name);

		/// <summary>
		/// Gets the boolean value with the specified name.
		/// </summary>
		/// <param name="name">The name of the boolean value.</param>
		/// <returns>The boolean value with the specified name, or false if not found.</returns>
		public bool GetBoolValue(string name) => _boolParameters.ContainsKey(name) && _boolParameters[name];

		/// <summary>
		/// Gets the integer value with the specified name.
		/// </summary>
		/// <param name="name">The name of the integer value.</param>
		/// <returns>The integer value with the specified name, or 0 if not found.</returns>
		public int GetIntValue(string name) => _intParameters.ContainsKey(name) ? _intParameters[name] : 0;

		/// <summary>
		/// Gets the float value with the specified name.
		/// </summary>
		/// <param name="name">The name of the float value.</param>
		/// <returns>The float value with the specified name, or 0 if not found.</returns>
		public float GetFloatValue(string name) => _floatParameters.ContainsKey(name) ? _floatParameters[name] : 0;

		public void ResetTriggers() => _triggers.Clear();

		/// <summary>
		/// Sets the specified trigger state.
		/// </summary>
		/// <param name="name">The name of the trigger state.</param>
		public void SetTrigger(string name) => _triggers.Add(name);

		/// <summary>
		/// Sets the specified boolean value.
		/// </summary>
		/// <param name="name">The name of the boolean value.</param>
		/// <param name="value">The value to set.</param>
		public void SetBool(string name, bool value) => _boolParameters[name] = value;

		/// <summary>
		/// Sets the specified integer value.
		/// </summary>
		/// <param name="name">The name of the integer value.</param>
		/// <param name="value">The value to set.</param>
		public void SetInt(string name, int value) => _intParameters[name] = value;

		/// <summary>
		/// Sets the specified float value.
		/// </summary>
		/// <param name="name">The name of the float value.</param>
		/// <param name="value">The value to set.</param>
		public void SetFloat(string name, float value) => _floatParameters[name] = value;

		/// <summary>
		/// Resets all parameters and triggers.
		/// </summary>
		public void Reset()
		{
			_boolParameters.Clear();
			_intParameters.Clear();
			_floatParameters.Clear();
			_triggers.Clear();
		}
	}
}
