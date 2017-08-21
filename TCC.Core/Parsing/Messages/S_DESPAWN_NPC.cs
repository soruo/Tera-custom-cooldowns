﻿using Tera.Game;
using Tera.Game.Messages;

namespace TCC.Parsing.Messages
{
    public class S_DESPAWN_NPC : ParsedMessage
    {
        public ulong Target { get; private set; }

        public S_DESPAWN_NPC(TeraMessageReader reader) : base(reader)
        {
            Target = reader.ReadUInt64();
        }
    }
}
