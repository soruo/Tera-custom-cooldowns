﻿using Tera.Game;
using Tera.Game.Messages;

namespace TCC.Parsing.Messages
{
    public class S_ABNORMALITY_END : ParsedMessage
    {
        public ulong TargetId { get; private set; }
        public uint AbnormalityId { get; private set; }

        public S_ABNORMALITY_END(TeraMessageReader reader) : base(reader)
        {
            TargetId = reader.ReadUInt64();
            AbnormalityId = reader.ReadUInt32();
        }
    }
}
