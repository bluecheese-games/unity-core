//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using System;
using UnityEngine;

namespace BlueCheese.Core.Utils
{

	[CreateAssetMenu(menuName = "Collection")]
	public class Collection : AssetBase
	{
		[SerializeField, HideInInspector] private Type _collectionType;
		[SerializeField, HideInInspector] private int[] _ints;
		[SerializeField, HideInInspector] private float[] _floats;
		[SerializeField, HideInInspector] private string[] _strings;
		[SerializeField, HideInInspector] private UnityEngine.Object[] _objects;

		public Type CollectionType => _collectionType;
		public int Size => GetSize();

		private int GetSize() => _collectionType switch
		{
			Type.Int => _ints.Length,
			Type.Float => _floats.Length,
			Type.String => _strings.Length,
			Type.Object => _objects.Length,
			_ => 0
		};

		public int GetInt(int index) => _ints[index];
		public int GetInt(int index, int defaultValue) => index >= 0 && index < _ints.Length ? _ints[index] : defaultValue;
		public int GetRandomInt() => _ints[UnityEngine.Random.Range(0, _ints.Length)];
		public ReadOnlySpan<int> Ints => _ints;

		public float GetFloat(int index) => _floats[index];
		public float GetFloat(int index, float defaultValue) => index >= 0 && index < _floats.Length ? _floats[index] : defaultValue;
		public float GetRandomFloat() => _floats[UnityEngine.Random.Range(0, _floats.Length)];
		public ReadOnlySpan<float> Floats => _floats;

		public string GetString(int index) => _strings[index];
		public string GetString(int index, string defaultValue) => index >= 0 && index < _strings.Length ? _strings[index] : defaultValue;
		public string GetRandomString() => _strings[UnityEngine.Random.Range(0, _strings.Length)];
		public ReadOnlySpan<string> Strings => _strings;

		public UnityEngine.Object GetObject(int index) => _objects[index];
		public UnityEngine.Object GetObject(int index, UnityEngine.Object defaultValue) => index >= 0 && index < _objects.Length ? _objects[index] : defaultValue;
		public UnityEngine.Object GetRandomObject() => _objects[UnityEngine.Random.Range(0, _objects.Length)];
		public ReadOnlySpan<UnityEngine.Object> Objects => _objects;

		public T GetObject<T>(int index) where T : UnityEngine.Object => GetObject(index) as T;
		public T GetObject<T>(int index, T defaultValue) where T : UnityEngine.Object => GetObject(index, defaultValue) as T;
		public T GetRandomObject<T>() where T : UnityEngine.Object => GetRandomObject() as T;
		public ReadOnlySpan<T> GetObjects<T>() where T : UnityEngine.Object => Array.ConvertAll(_objects, item => item as T);

		public enum Type
		{
			Int,
			Float,
			String,
			Object
		}
	}
}
