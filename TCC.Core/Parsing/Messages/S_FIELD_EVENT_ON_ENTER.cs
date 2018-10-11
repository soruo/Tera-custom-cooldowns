﻿using TCC.TeraCommon.Game.Messages;
using TCC.TeraCommon.Game.Services;

namespace TCC.Parsing.Messages
{
    public class S_FIELD_EVENT_ON_ENTER : ParsedMessage
    {
        public S_FIELD_EVENT_ON_ENTER(TeraMessageReader reader) : base(reader)
        {
        }
    }

    public class S_FIELD_EVENT_ON_LEAVE : ParsedMessage
    {
        public S_FIELD_EVENT_ON_LEAVE(TeraMessageReader reader) : base(reader)
        {
        }
    }
}
