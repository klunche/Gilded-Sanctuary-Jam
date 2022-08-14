using RWCustom;
using UnityEngine;

namespace GoldenRegionJam;

public class LeviathanPushbackObject : UpdatableAndDeletable
{
	public PlacedObject.ResizableObjectData data;

	public Vector2 Pos => data.owner.pos;

	public float Rad => data.handlePos.magnitude;

	public Vector2 Dir => data.handlePos.normalized;

	public LeviathanPushbackObject(Room room, PlacedObject.ResizableObjectData data)
	{
		base.room = room;
		this.data = data;
	}

	public override void Update(bool eu)
	{
		base.Update(eu);
		for (var i = 0; i < room.abstractRoom.creatures.Count; i++)
		{
			var cr = room.abstractRoom.creatures[i];
			if (cr.realizedCreature is not BigEel || cr.realizedCreature.room != room || !cr.realizedCreature.Consious || cr.realizedCreature.grabbedBy.Count != 0)
				continue;
			for (var j = 0; j < cr.realizedCreature.bodyChunks.Length; j++)
			{
				ref readonly var chunk = ref cr.realizedCreature.bodyChunks[j];
				if (Custom.DistLess(Custom.RestrictInRect(chunk.pos, room.RoomRect), Pos, Rad))
					chunk.vel += Dir * 5f * Mathf.InverseLerp(Rad, Rad - 60f, Vector2.Distance(Custom.RestrictInRect(chunk.pos, room.RoomRect), Pos));
			}
		}
	}
}
