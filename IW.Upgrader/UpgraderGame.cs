using IW.Upgrader.Card;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace IW.Upgrader;

public sealed class UpgraderGame {
	private static readonly byte[] SaveFormat = Encoding.ASCII.GetBytes("IWUP14");
	public const int InputLimit = 6;
	public const float MinUpgradeChance = .001f;
	public const float MaxUpgradeChance = .799f;
	public const int MaxPackCards = 3;

	private readonly Random _rng;
	private readonly List<CollectableCard> _catalog = [];
	private readonly List<CollectableCard> _inv = [];
	private readonly CollectableCard[] _upgInput = new CollectableCard[InputLimit];
	private readonly List<UpgradeInfo> _upgHistory = [];
	private readonly List<CollectableCardPack> _cardPacks = [];
	private readonly TimeSpan _savedElapsedTime = TimeSpan.Zero;
	private readonly DateTimeOffset _sessionStartTime;

	public float Balance { get; private set; }
	public int SourceSeed { get; private set; }
	public DateTimeOffset StartTime { get; init; }
	public TimeSpan ElapsedTime => _savedElapsedTime + (DateTimeOffset.UtcNow - _sessionStartTime);
	public IReadOnlyList<CollectableCard> UpgradeInput => _upgInput;
	public IReadOnlyList<CollectableCard> Inventory => _inv;
	public IReadOnlyList<CollectableCard> Catalog => _catalog;
	public IReadOnlyList<UpgradeInfo> UpgradeHistory => _upgHistory;
	public IReadOnlyList<CollectableCardPack> CardPacks => _cardPacks;
	public UpgradeInfo? BestUpgrade => _upgHistory.Where(i => i.Result).OrderBy(i => i.DropItem.price).FirstOrDefault();
	public UpgraderGame(IEnumerable<CollectableCardType> itemTypes, IEnumerable<CollectableCardPack> itemPacks, float balance, int sourceSeed) {
		SourceSeed = sourceSeed;
		_rng = new(sourceSeed);
		foreach (var cType in itemTypes) {
			var rarities = Enum.GetValues<CollectableCard.Rarity>();
			for (int i = 0; i < rarities.Length; i++) _catalog.Add(new(cType, rarities[i]));
		}
		foreach (var p in itemPacks) {
			p.InitCardSet(_catalog);
			_cardPacks.Add(p);
		}
		Balance = balance;
		StartTime = DateTime.UtcNow;
		_sessionStartTime = StartTime;
	}

	public UpgraderGame(IEnumerable<CollectableCardType> itemTypes, IEnumerable<CollectableCardPack> itemPacks, float balance) : this(itemTypes, itemPacks, balance, 0) {
		var bytes = new byte[4];
		using (var csprng = RandomNumberGenerator.Create()) csprng.GetBytes(bytes);
		SourceSeed = BitConverter.ToInt32(bytes, 0);
		_rng = new(SourceSeed);
	}

	public UpgraderGame(IEnumerable<CollectableCardType> itemTypes, IEnumerable<CollectableCardPack> itemPacks, byte[] data) : this(itemTypes, itemPacks, 0) {
		using MemoryStream ms = new(data);
		using BinaryReader rd = new(ms);
		byte[] sf = rd.ReadBytes(SaveFormat.Length);
		if (!sf.SequenceEqual(SaveFormat))
			throw new ArgumentException($"Incorrect save format", nameof(data));
		SourceSeed = rd.ReadInt32();
		Balance = rd.ReadSingle();
		StartTime = new(rd.ReadInt64(), TimeSpan.Zero);
		_savedElapsedTime = new(rd.ReadInt64());
		_inv = [.. rd.ReadCollection(ReadCollectableCard)];
		_upgInput = [.. rd.ReadCollection(ReadCollectableCard)];
		_upgHistory = [.. rd.ReadCollection(ReadUpgradeInfo)];
		_rng = new(SourceSeed);
		_upgHistory.ForEach(_ => _rng.Next());
		_sessionStartTime = DateTimeOffset.UtcNow;
	}

	public byte[] Save() {
		using MemoryStream ms = new();
		using BinaryWriter wr = new(ms);
		wr.Write(SaveFormat);
		wr.Write(SourceSeed);
		wr.Write(Balance);
		wr.Write(StartTime.UtcTicks);
		wr.Write(ElapsedTime.Ticks);
		wr.WriteCollection(_inv, WriteCollectableCard);
		wr.WriteCollection(_upgInput, WriteCollectableCard);
		wr.WriteCollection(_upgHistory, WriteUpgradeInfo);
		return ms.ToArray();
	}
	private static CollectableCard ReadCollectableCard(BinaryReader rd) {
		if (rd.ReadByte() == 0) return default;
		rd.BaseStream.Position -= 1;
		var rarities = Enum.GetValues<CollectableCard.Rarity>();
		return new(new() {
			Id = rd.ReadString(),
			CollectionIds = [.. rd.ReadCollection(rd => rd.ReadString())],
			BasePrice = rd.ReadSingle(),
			MinRarity = rarities[rd.ReadByte()],
			MaxRarity = rarities[rd.ReadByte()],

		}, rarities[rd.ReadByte()]);
	}

	private static void WriteCollectableCard(BinaryWriter wr, CollectableCard card) {
		if (card.type is null) {
			wr.Write((byte)0);
			return;
		}
		wr.Write(card.type.Id);
		wr.WriteCollection(card.type.CollectionIds, (wr, s) => wr.Write(s));
		wr.Write(card.type.BasePrice);
		wr.Write((byte)card.type.MinRarity);
		wr.Write((byte)card.type.MaxRarity);
		wr.Write((byte)card.rarity);
	}

	private static UpgradeInfo ReadUpgradeInfo(BinaryReader rd) => new() {
		InputItems = [.. rd.ReadCollection(ReadCollectableCard)],
		DropItem = ReadCollectableCard(rd),
		Chance = rd.ReadSingle(),
		Result = rd.ReadBoolean()
	};

	private static void WriteUpgradeInfo(BinaryWriter wr, UpgradeInfo info) {
		wr.WriteCollection(info.InputItems, WriteCollectableCard);
		WriteCollectableCard(wr, info.DropItem);
		wr.Write(info.Chance);
		wr.Write(info.Result);
	}

	public bool BuyPack(int packIdx, [MaybeNullWhen(false)] out IEnumerable<CollectableCard> result) {
		var pack = _cardPacks[packIdx];
		if (Balance < pack.Price) {
			result = null;
			return false;
		}

		Balance -= pack.Price;
		var cardSet = pack.CardSet.OrderByDescending(i => i.price).ToArray();

		if (cardSet.Length == 0) {
			result = [];
			return true;
		}

		float[] weights = new float[cardSet.Length];
		float totalWeight = 0f;
		float steepness = 1f + cardSet[0].price / cardSet.Sum(i => i.price);

		for (int i = 0; i < cardSet.Length; i++) {
			weights[i] = 1f / MathF.Pow(cardSet[i].price, steepness);
			totalWeight += weights[i];
		}
		CollectableCard[] cards = new CollectableCard[Math.Min(cardSet.Length, MaxPackCards)];

		for (int i = 0; i < cards.Length; i++) {
			float next = _rng.NextSingle() * totalWeight;
			float cumulative = 0f;

			cards[i] = cardSet[^1];

			for (int j = 0; j < cardSet.Length; j++) {
				cumulative += weights[j];
				if (next > cumulative) continue;
				cards[i] = cardSet[j];
				break;
			}
		}

		result = cards;
		_inv.AddRange(result);
		return true;
	}

	public void Sell(int invIndex) {
		var item = _inv[invIndex];
		_inv.RemoveAt(invIndex);
		Balance += item.price;
	}

	public bool BuyCard(int ctgIndex) {
		var item = _catalog[ctgIndex];
		if (Balance < item.price || !item.IsPurchaseable) return false;
		Balance -= item.price;
		_inv.Add(item);
		return true;
	}

	public void Put(int invIndex) {
		var item = _inv[invIndex];
		_inv.RemoveAt(invIndex);
		_upgInput[Array.FindIndex(_upgInput, c => c.type is null)] = item;
	}

	public void UndoPut() {
		_inv.AddRange(_upgInput);
		Array.Clear(_upgInput);
	}

	public float GetDropChance(CollectableCard target) => _upgInput.Sum(c => c.price) / target.price;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ValidChance(float chance) => chance <= MaxUpgradeChance && chance >= MinUpgradeChance;

	public bool Upgrade(int ctgndex) {
		var dropItem = _catalog[ctgndex];
		float chance = GetDropChance(dropItem);
		if (!ValidChance(chance))
			return true;
		bool result = false;
		if (_rng.NextSingle() <= chance) {
			_inv.Add(dropItem);
			result = true;
		}
		_upgHistory.Add(new() {
			InputItems = [.. _upgInput],
			DropItem = dropItem,
			Chance = chance,
			Result = result
		});
		Array.Clear(_upgInput);
		return result;
	}
}
