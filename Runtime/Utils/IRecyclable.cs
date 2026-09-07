namespace BlueCheese.Core.Utils
{
	public interface IRecyclable
    {
		/// <summary>
		/// Called when the object is being recycled. Implement this method to reset the object's state as needed.
		/// </summary>
		void OnRecycle();
    }
}
