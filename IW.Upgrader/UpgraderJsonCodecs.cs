using IW.Upgrader.Card;
using SonicD5.Json;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IW.Upgrader; 
public static class UpgraderJsonCodecs {
	public static readonly JsonCodec CollectableCardPackCodec = new() {
		TPredicate = (ref ctx) => ctx.Type.IsAssignableTo(typeof(CollectableCardPack)),
		JsonTypes = JsonTypes.Object,
		DCallback = (ref ctx) => JsonCodec.DeserializeObject(ref ctx, ctx.Type.Value.GetFieldsAndProperties(Lele, BindingFlags.Public | BindingFlags.NonPublic)),
		SCallback = (ref ctx) => JsonCodec.SerializeObject(ref ctx, ctx.Type.Value.GetFieldsAndProperties(JsonCodec.DefaultMemberFilter))
	};

	private static bool Lele(MemberInfo m) =>
	!m.IsDefined(typeof(JsonSerializationIgnoreAttribute), true)
	&& (
		(m.MemberType == MemberTypes.Field
			&& !m.IsDefined(typeof(CompilerGeneratedAttribute), false))
		|| (m is PropertyInfo p
			&& p.CanRead
			&& p.CanWrite)
	);
}
