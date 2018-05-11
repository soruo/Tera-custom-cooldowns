﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TCC.Data;
using TCC.Data.Databases;
using TCC.Parsing.Messages;
using TCC.ViewModels;

namespace TCC.ClassSpecific
{
    public static class Mystic
    {
        private const int HurricaneId = 60010;
        private const int HurricaneDuration = 120000;
        private static readonly int VowId = 700100;

        public static List<uint> CommonBuffs = new List<uint>
        {
            700100,                 //vow
            27120,                  //united thrall of protection
            601, 602, 603,          //titanic wrath
            700630,700631,          //titanic fury
            700230, 700231, 700232, 700233, //aura unyelding
            700330,                 //aura mp
            700730, 700731          //aura swift
        };

        private static readonly uint[] CritAuraIDs = { 700600, 700601, 700602, 700603 };
        private static readonly uint[] ManaAuraIDs = { 700300 };
        private static readonly uint[] CritResAuraIDs = { 700200, 700201, 700202, 700203 };
        private static readonly uint[] SwiftAuraIDs = { 700700, 700701 };
        private static readonly uint[] ElementalizeIDs = { 702000 };

        public static void CheckHurricane(S_ABNORMALITY_BEGIN msg)
        {
            if (msg.AbnormalityId != HurricaneId || msg.CasterId != SessionManager.CurrentPlayer.EntityId) return;
            SessionManager.SkillsDatabase.TryGetSkill(HurricaneId, Class.Common, out var hurricane);
            SkillManager.AddSkillDirectly(hurricane, HurricaneDuration);

        }
        public static void CheckBuff(S_ABNORMALITY_BEGIN p)
        {
            if (p.TargetId != SessionManager.CurrentPlayer.EntityId) return;
            if (CritAuraIDs.Contains(p.AbnormalityId))
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.CritAura = true;
            }
            else if (ManaAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.ManaAura = true;
            }
            else if (CritResAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.CritResAura = true;
            }
            else if (SwiftAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.SwiftAura = true;
            }
            else if (p.AbnormalityId == VowId )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Vow.Buff.Start(p.Duration);
            }
            else if (ElementalizeIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Elementalize = true;
            }

        }
        public static void CheckBuff(S_ABNORMALITY_REFRESH p)
        {
            if (p.TargetId != SessionManager.CurrentPlayer.EntityId) return;

            if (CritAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.CritAura = true;
            }
            else if (ManaAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.ManaAura = true;
            }
            else if (CritResAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.CritResAura = true;
            }
            else if (SwiftAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.SwiftAura = true;
            }
            else if (p.AbnormalityId == VowId )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Vow.Buff.Refresh(p.Duration);
            }
            else if (ElementalizeIDs.Contains(p.AbnormalityId))
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Elementalize = true;
            }
        }
        public static void CheckBuffEnd(S_ABNORMALITY_END p)
        {
            if (p.TargetId != SessionManager.CurrentPlayer.EntityId) return;

            if (CritAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.CritAura = false;
            }
            else if (ManaAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.ManaAura = false;
            }
            else if (CritResAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.CritResAura = false;
            }
            else if (SwiftAuraIDs.Contains(p.AbnormalityId) )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Auras.SwiftAura = false;
            }
            else if (p.AbnormalityId == VowId )
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Vow.Buff.Refresh(0);
            }
            else if (ElementalizeIDs.Contains(p.AbnormalityId))
            {
                ((MysticBarManager)ClassWindowViewModel.Instance.CurrentManager).Elementalize = false;
            }
        }

    }
}
