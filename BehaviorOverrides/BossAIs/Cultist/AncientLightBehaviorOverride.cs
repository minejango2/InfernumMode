using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class AncientLightBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.AncientLight;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            int swerveTime = 42;
            bool phase2Variant = npc.ai[0] == 1f;
            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];

            if (attackTimer <= swerveTime)
            {
                float moveOffsetAngle = (float)Math.Cos(npc.Center.Length() / 150f + npc.whoAmI % 10f / 10f * MathHelper.TwoPi);
                moveOffsetAngle *= MathHelper.Pi * 0.85f / swerveTime;

                npc.velocity = npc.velocity.RotatedBy(moveOffsetAngle) * 0.97f;
            }
            else
            {
                if (npc.WithinRange(target.Center, 700f))
                {
                    bool canNoLongerHome = attackTimer >= swerveTime + 125f;
                    float idealSpeed = canNoLongerHome ? 30f : 23f;
                    if (BossRushEvent.BossRushActive)
                        idealSpeed *= 1.425f;

                    float newSpeed = MathHelper.Clamp(npc.velocity.Length() + (canNoLongerHome ? 0.075f : 0.024f), 13f, idealSpeed);
                    if (!target.dead && target.active && !npc.WithinRange(target.Center, 320f) && !canNoLongerHome)
                    {
                        float homingPower = phase2Variant ? 0.067f : 0.062f;
                        npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), homingPower);
                    }
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * newSpeed;
                }
                else
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * npc.velocity.Length(), 0.2f).SafeNormalize(Vector2.UnitY) * (BossRushEvent.BossRushActive ? 29f : 17f);

                // Die on tile collision or after enough time.
                bool shouldDie = (Collision.SolidCollision(npc.position, npc.width, npc.height) && attackTimer >= swerveTime + 95f) || attackTimer >= swerveTime + 240f;
                if (attackTimer >= swerveTime + 90f && shouldDie)
                {
                    npc.HitEffect(0, 9999.0);
                    npc.active = false;
                    npc.netUpdate = true;
                }
            }
            attackTimer++;

            npc.rotation = npc.velocity.ToRotation() - MathHelper.PiOver2;
            npc.dontTakeDamage = true;
            return false;
        }
    }
}
