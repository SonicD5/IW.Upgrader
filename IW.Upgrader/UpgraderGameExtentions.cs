using IW.Upgrader.Card;
using System.Text;

namespace IW.Upgrader; 
internal static class UpgraderGameExtentions {

	public static ICollection<T> ReadCollection<T>(this BinaryReader rd, Func<BinaryReader, T> eReader) {
		int count = rd.ReadInt32();
		T[] values = new T[count];
		for (int i = 0; i < count; i++)
			values[i] = eReader(rd);
		return values;
	}

	public static void WriteCollection<T>(this BinaryWriter wr, IList<T> collection, Action<BinaryWriter, T> eWriter) {
		wr.Write(collection.Count);
		for (int i = 0; i < collection.Count; i++)
			eWriter(wr, collection[i]);
	}
}
