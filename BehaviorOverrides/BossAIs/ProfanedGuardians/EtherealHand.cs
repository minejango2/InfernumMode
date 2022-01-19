using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class EtherealHand : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float HandSide => ref npc.ai[0];
        public ref float FingerOutwardness => ref npc.localAI[0];
        public ref float FingerSpacingOffset => ref npc.localAI[1];
        public bool UsingPointerFinger
        {
            get => npc.localAI[2] != 0f;
            set => npc.localAI[2] = value.ToInt();
        }

        internal NPC AttackerGuardian => Main.npc[CalamityGlobalNPC.doughnutBoss];
        internal bool ShouldBeInvisible => AttackerGuardian.localAI[2] != 0f;
        internal float AttackTime => AttackerGuardian.ai[1];
        internal AttackerGuardianBehaviorOverride.AttackGuardianAttackState AttackerState => (AttackerGuardianBehaviorOverride.AttackGuardianAttackState)(int)AttackerGuardian.ai[0];
        internal Vector2 PointerFingerPosition => npc.Center + (npc.rotation + FingerSpacingOffset * -5f).ToRotationVector2() * FingerOutwardness;

        internal const float HandSize = 56f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ethereal Hand");
            NPCID.Sets.TrailingMode[npc.type] = 2;
            NPCID.Sets.TrailCacheLength[npc.type] = 15;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.damage = 100;
            npc.width = npc.height = 50;
            npc.dontTakeDamage = true;
            npc.lifeMax = 10000;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.hide = true;
            npc.alpha = 255;
        }

        public override void AI()
        {
            if (CalamityGlobalNPC.doughnutBoss < 0 || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            if (ShouldBeInvisible)
                npc.alpha = Utils.Clamp(npc.alpha + 40, 0, 255);
            else
                npc.alpha = Utils.Clamp(npc.alpha - 12, 0, 255);

            Vector2 destination = Vector2.Zero;
            npc.target = Main.npc[CalamityGlobalNPC.doughnutBoss].target;
            npc.rotation = AttackerGuardian.AngleTo(npc.Center);

            UsingPointerFinger = false;
            destination = AttackerGuardian.Center + new Vector2(110f * HandSide, -120f + 30f * (float)Math.Sin(AttackTime / 16f + HandSide * 2.1f));

            FingerOutwardness = 34f;
            FingerSpacingOffset = MathHelper.Lerp(FingerSpacingOffset, MathHelper.ToRadians(9f), 0.25f);

            if (ShouldBeInvisible)
                destination = AttackerGuardian.Center + AttackerGuardian.SafeDirectionTo(npc.Center);

            npc.velocity = npc.SafeDirectionTo(destination) * MathHelper.Min(16f + (AttackerGuardian.position - AttackerGuardian.oldPos[1]).Length() * 2f, npc.Distance(destination));
            if (npc.velocity.HasNaNs())
                npc.velocity = Vector2.UnitY;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = ModContent.GetTexture("CalamityMod/Projectiles/StarProj");
            Vector2 handScale = new Vector2(HandSize) / texture.Size() * 1.6f;
            SpriteEffects direction = HandSide == 1f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Color handColor = Color.Lerp(Color.Orange, Color.Yellow, 0.5f);
            handColor = Color.Lerp(handColor, Color.LightGoldenrodYellow, 0.5f);
            handColor *= npc.Opacity * AttackerGuardian.Opacity;
            handColor.A = 0;

            float distanceFromAttacker = npc.Distance(AttackerGuardian.Center);
            int totalPoints = 20 + (int)(distanceFromAttacker / 40f);

            Vector2 sagLocation = Vector2.Lerp(AttackerGuardian.Center, npc.Center, 0.5f);
            sagLocation.Y += AttackerGuardian.velocity.Y * -10f;
            sagLocation.Y += MathHelper.Lerp(0f, 60f, Utils.InverseLerp(4f, 1f, Math.Abs(AttackerGuardian.velocity.Y), true));

            Vector2[] drawPoints = new BezierCurveCopy(AttackerGuardian.Center, sagLocation, npc.Center).GetPoints(totalPoints).ToArray();

            for (int i = 0; i < 5; i++)
            {
                float fingerAngle = npc.rotation + MathHelper.Lerp(-5f, 5f, i / 5f) * FingerSpacingOffset;
                float universalScaleFactor = i != 0 && UsingPointerFinger ? 0f : 1f;
                float currentFingerOutwardness = FingerOutwardness * universalScaleFactor;
                Vector2 fingerScale = new Vector2(currentFingerOutwardness / 3f) / texture.Size() * new Vector2(4f, 3f) * universalScaleFactor;

                for (int j = 0; j < 3; j++)
                {
                    Vector2 fingerDrawPosition = npc.Center + fingerAngle.ToRotationVector2() * HandSize * 0.5f - Main.screenPosition;
                    fingerDrawPosition += fingerAngle.ToRotationVector2() * currentFingerOutwardness * j / 3f;
                    spriteBatch.Draw(texture, fingerDrawPosition, null, handColor, fingerAngle + MathHelper.PiOver2, texture.Size() * new Vector2(0.5f, 0f), fingerScale, direction, 0f);
                }
            }

            for (int i = 0; i < 30; i++)
            {
                float handRotation = npc.rotation + MathHelper.PiOver2 + MathHelper.TwoPi * i / 30f;
                spriteBatch.Draw(texture, npc.Center - Main.screenPosition, null, handColor * 0.08f, handRotation, texture.Size() * 0.5f, handScale, direction, 0f);
            }

            for (int i = 0; i < drawPoints.Length - 1; i++)
            {
                float completionRatio = i / (float)drawPoints.Length;

                Vector2 currentPoint = drawPoints[i];
                Vector2 nextPoint = drawPoints[i + 1];
                Vector2 midPoint = Vector2.Lerp(currentPoint, nextPoint, 0.5f);

                if (i > 8 && Main.rand.NextBool(50) && npc.Opacity * AttackerGuardian.Opacity == 1f && !ShouldBeInvisible)
                {
                    Dust fire = Dust.NewDustPerfect(currentPoint, 244);
                    fire.color = Color.Yellow;
                    fire.velocity = Vector2.UnitY * -Main.rand.NextFloat(1f, 1.2f);
                    fire.velocity += npc.velocity * new Vector2(1f, 0.3f);
                    fire.scale = 0.8f;
                }

                float rotation = (nextPoint - currentPoint).ToRotation() + MathHelper.PiOver2;
                Vector2 segmentScale = handScale * MathHelper.Lerp(0.6f, 1f, Utils.InverseLerp(0.36f, 0f, completionRatio, true));

                spriteBatch.Draw(texture, currentPoint - Main.screenPosition, null, handColor * 0.5f, rotation, texture.Size() * 0.5f, segmentScale, direction, 0f);
                spriteBatch.Draw(texture, midPoint - Main.screenPosition, null, handColor * 0.5f, rotation, texture.Size() * 0.5f, segmentScale, direction, 0f);
            }

            return false;
        }

        public override bool CheckActive() => false;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = 1;
            return true;
        }

        public override void OnHitPlayer(Player player, int damage, bool crit)
        {
            player.AddBuff(ModContent.BuffType<HolyFlames>(), 120, true);
        }
    }
}
