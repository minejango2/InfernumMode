﻿using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresLaserCannonBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AresLaserCannon>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
            {
                npc.active = false;
                return false;
            }

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            ExoMechAIUtilities.HaveArmsInheritAresBodyAttributes(npc);

            Player target = Main.player[npc.target];

            // Define attack variables.
            bool currentlyDisabled = AresBodyBehaviorOverride.ArmIsDisabled(npc);
            int shootTime = 300;
            int totalLasersPerBurst = 5;
            float aimPredictiveness = 25f;
            float laserShootSpeed = 7.5f;
            ref float attackTimer = ref npc.ai[0];
            ref float chargeDelay = ref npc.ai[1];
            ref float laserCounter = ref npc.ai[2];
            ref float currentDirection = ref npc.ai[3];
            int laserCount = laserCounter % 3f == 2f ? 3 : 1;

            if (ExoMechManagement.CurrentAresPhase >= 2)
            {
                laserCount += 2;
                totalLasersPerBurst = 12;
                shootTime += 210;
                laserShootSpeed *= 1.1f;
            }

            if (ExoMechManagement.CurrentAresPhase >= 3)
            {
                laserShootSpeed *= 0.85f;

                if (laserCount == 3)
                    laserCount += 2;
            }

            // Nerf things while Ares' complement mech is present.
            if (ExoMechManagement.CurrentAresPhase == 4)
            {
                shootTime += 45;
                if (laserCount > 4)
                    laserCount = 4;
                laserCount--;
                laserShootSpeed *= 0.8f;
            }

            if (ExoMechManagement.CurrentAresPhase >= 5)
            {
                shootTime += 120;
                laserShootSpeed *= 0.8f;
            }

            // Get very pissed off if Ares is enraged.
            if (aresBody.Infernum().ExtraAI[13] == 1f)
            {
                shootTime /= 3;
                laserShootSpeed *= 1.5f;
            }

            int shootRate = shootTime / totalLasersPerBurst;

            // Initialize delays and other timers.
            if (chargeDelay == 0f)
                chargeDelay = AresBodyBehaviorOverride.Phase1ArmChargeupTime;

            // Don't do anything if this arm should be disabled.
            if (currentlyDisabled && attackTimer >= chargeDelay)
                attackTimer = chargeDelay;

            // Hover near Ares.
            bool doingHoverCharge = aresBody.ai[0] == (int)AresBodyBehaviorOverride.AresBodyAttackType.HoverCharge ||
                aresBody.ai[0] == (int)ExoMechComboAttackContent.ExoMechComboAttackType.AresTwins_ThermoplasmaDance;
            float horizontalOffset = doingHoverCharge ? 380f : 575f;
            float verticalOffset = doingHoverCharge ? 150f : 0f;
            Vector2 hoverDestination = aresBody.Center + new Vector2((aresBody.Infernum().ExtraAI[15] == 1f ? -1f : 1f) * -horizontalOffset, verticalOffset);
            ExoMechAIUtilities.DoSnapHoverMovement(npc, hoverDestination, 65f, 115f);
            npc.Infernum().ExtraAI[0] = MathHelper.Clamp(npc.Infernum().ExtraAI[0] + doingHoverCharge.ToDirectionInt(), 0f, 15f);

            // Check to see if this arm should be used for special things in a combo attack.
            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
            {
                float _ = 0f;
                ExoMechComboAttackContent.UseThanatosAresComboAttack(npc, ref aresBody.ai[1], ref _);
                ExoMechComboAttackContent.UseTwinsAresComboAttack(npc, 1f, ref aresBody.ai[1], ref _);
                return false;
            }

            // Calculate the direction and rotation this arm should use.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
            ExoMechAIUtilities.PerformAresArmDirectioning(npc, aresBody, target, aimDirection, currentlyDisabled, doingHoverCharge, ref currentDirection);

            float rotationToEndOfCannon = npc.rotation;
            if (rotationToEndOfCannon < 0f)
                rotationToEndOfCannon += MathHelper.Pi;
            Vector2 endOfCannon = npc.Center + rotationToEndOfCannon.ToRotationVector2() * 74f + Vector2.UnitY * 8f;

            // Determine direction based on rotation.
            npc.direction = (npc.rotation > 0f).ToDirectionInt();

            // Create a dust telegraph before firing.
            if (attackTimer > chargeDelay * 0.7f && attackTimer < chargeDelay)
            {
                Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                Dust laser = Dust.NewDustPerfect(dustSpawnPosition, 182);
                laser.velocity = (endOfCannon - laser.position) * 0.04f;
                laser.scale = 1.25f;
                laser.noGravity = true;
            }

            // Fire lasers.
            if (attackTimer >= chargeDelay && attackTimer % shootRate == shootRate - 1f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int laserDamage = AresBodyBehaviorOverride.ProjectileDamageBoost + 530;
                    for (int i = 0; i < laserCount; i++)
                    {
                        Vector2 laserShootVelocity = aimDirection * laserShootSpeed;
                        if (laserCount > 1)
                            laserShootVelocity = laserShootVelocity.RotatedBy(MathHelper.Lerp(-0.41f, 0.41f, i / (float)(laserCount - 1f)));
                        laserShootVelocity = laserShootVelocity.RotatedByRandom(0.07f);
                        int laser = Utilities.NewProjectileBetter(endOfCannon, laserShootVelocity, ModContent.ProjectileType<CannonLaser>(), laserDamage, 0f);
                        if (Main.projectile.IndexInRange(laser))
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                    }

                    laserCounter++;
                    npc.netUpdate = true;
                }
            }

            // Reset the attack and laser counter after an attack cycle ends.
            if (attackTimer >= chargeDelay + shootTime)
            {
                attackTimer = 0f;
                laserCounter = 0f;
                npc.netUpdate = true;
            }
            attackTimer++;
            return false;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            int currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] / npc.ai[1]));

            if (npc.ai[0] > npc.ai[1])
            {
                npc.frameCounter++;
                if (npc.frameCounter >= 66f)
                    npc.frameCounter = 0D;
                currentFrame = (int)Math.Round(MathHelper.Lerp(36f, 47f, (float)npc.frameCounter / 66f));
            }
            else
                npc.frameCounter = 0D;

            if (ExoMechComboAttackContent.ArmCurrentlyBeingUsed(npc))
                currentFrame = (int)Math.Round(MathHelper.Lerp(0f, 35f, npc.ai[0] % 72f / 72f));

            npc.frame = new Rectangle(npc.width * (currentFrame / 8), npc.height * (currentFrame % 8), npc.width, npc.height);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.Infernum().OptionalPrimitiveDrawer is null)
            {
                npc.Infernum().OptionalPrimitiveDrawer = new PrimitiveTrailCopy(completionRatio => AresBodyBehaviorOverride.FlameTrailWidthFunctionBig(npc, completionRatio),
                    completionRatio => AresBodyBehaviorOverride.FlameTrailColorFunctionBig(npc, completionRatio),
                    null, true, GameShaders.Misc["Infernum:TwinsFlameTrail"]);
            }

            for (int i = 0; i < 2; i++)
            {
                if (npc.Infernum().ExtraAI[0] > 0f)
                    npc.Infernum().OptionalPrimitiveDrawer.Draw(npc.oldPos, npc.Size * 0.5f - Main.screenPosition, 54);
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = npc.Center - Main.screenPosition;
            Color afterimageBaseColor = aresBody.Infernum().ExtraAI[13] == 1f ? Color.Red : Color.White;
            int numAfterimages = 5;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }
            float finalPhaseGlowInterpolant = Utils.InverseLerp(0f, ExoMechManagement.FinalPhaseTransitionTime * 0.75f, aresBody.Infernum().ExtraAI[ExoMechManagement.FinalPhaseTimerIndex], true);
            if (finalPhaseGlowInterpolant > 0f)
            {
                float backAfterimageOffset = finalPhaseGlowInterpolant * 6f;
                for (int i = 0; i < 8; i++)
                {
                    Color color = Main.hslToRgb((i / 8f + Main.GlobalTime * 0.6f) % 1f, 1f, 0.56f) * 0.5f;
                    color.A = 0;
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 0.8f + 0.25f).ToRotationVector2() * backAfterimageOffset;
                    spriteBatch.Draw(texture, center + drawOffset, frame, npc.GetAlpha(color), npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }
            spriteBatch.Draw(texture, center, frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresLaserCannonGlow");

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < numAfterimages; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, afterimageBaseColor, 0.5f)) * ((numAfterimages - i) / 15f);
                    Vector2 afterimageCenter = npc.oldPos[i] + npc.frame.Size() * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.oldRot[i], origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(texture, center, frame, afterimageBaseColor * npc.Opacity, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}
