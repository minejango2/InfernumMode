﻿using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Tiles;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    // It begins.
    public class SupremeCalamitasBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalAttackType
		{
            AcceleratingRedirectingSkulls,
            MagicChargeBlasts,
            DarkMagicFireballFan,
            SwervingBlasts
        }
        public override int NPCOverrideType => ModContent.NPCType<SCalBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public static readonly Vector2 SepulcherSpawnOffset = new Vector2(0f, -350f);

        #region Pattern Lists

        public const float Phase2LifeRatio = 0.75f;
        public const float Phase3LifeRatio = 0.5f;

        public static readonly SCalAttackType[] Subphase1Pattern = new SCalAttackType[]
        {
            SCalAttackType.AcceleratingRedirectingSkulls,
            SCalAttackType.DarkMagicFireballFan,
            SCalAttackType.SwervingBlasts,
            SCalAttackType.MagicChargeBlasts,
            SCalAttackType.AcceleratingRedirectingSkulls,
            SCalAttackType.DarkMagicFireballFan,
            SCalAttackType.AcceleratingRedirectingSkulls,
            SCalAttackType.SwervingBlasts,
            SCalAttackType.MagicChargeBlasts,
            SCalAttackType.DarkMagicFireballFan,
        };
        public static readonly SCalAttackType[] Subphase2Pattern = new SCalAttackType[]
        {
            SCalAttackType.AcceleratingRedirectingSkulls,
            SCalAttackType.DarkMagicFireballFan,
            SCalAttackType.SwervingBlasts,
            SCalAttackType.MagicChargeBlasts,
            SCalAttackType.AcceleratingRedirectingSkulls,
            SCalAttackType.DarkMagicFireballFan,
            SCalAttackType.AcceleratingRedirectingSkulls,
            SCalAttackType.SwervingBlasts,
            SCalAttackType.MagicChargeBlasts,
            SCalAttackType.DarkMagicFireballFan,
        };
        public static readonly Dictionary<SCalAttackType[], Func<NPC, bool>> SubphaseTable = new Dictionary<SCalAttackType[], Func<NPC, bool>>()
        {
            [Subphase1Pattern] = (npc) => npc.life / (float)npc.lifeMax >= Phase2LifeRatio,
            [Subphase2Pattern] = (npc) => npc.life / (float)npc.lifeMax < Phase2LifeRatio && npc.life / (float)npc.lifeMax >= Phase3LifeRatio,
        };

        public static SCalAttackType CurrentAttack(NPC npc)
        {
            SCalAttackType[] patternToUse = SubphaseTable.First(table => table.Value(npc)).Key;
            return patternToUse[(int)npc.ai[0] % patternToUse.Length];
        }
        #endregion Pattern Lists

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);

                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(npc.Center, (int)CalamityDusts.Brimstone);
                    fire.position += Main.rand.NextVector2Circular(36f, 36f);
                    fire.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    fire.noGravity = true;
                    fire.scale *= Main.rand.NextFloat(1f, 1.2f);
                }

                if (npc.Opacity <= 0f)
                    npc.active = false;
                return false;
            }

            // Set the whoAmI index.
            CalamityGlobalNPC.SCal = npc.whoAmI;

            // Reset things.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool sepulcherIsPresent = NPC.AnyNPCs(ModContent.NPCType<SCalWormHead>());
            ref float attackCycleIndex = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float attackTextDelay = ref npc.ai[2];
            ref float textState = ref npc.ai[3];
            ref float initialChargeupTime = ref npc.Infernum().ExtraAI[5];
            ref float currentPhase = ref npc.Infernum().ExtraAI[6];

            // Handle initializations.
            if (npc.localAI[1] == 0f)
            {
                // Teleport above the player.
                Vector2 oldPosition = npc.Center;
                npc.Center = target.Center - Vector2.UnitY * 160f;
                Dust.QuickDustLine(oldPosition, npc.Center, 300f, Color.Red);

                // Define the arena.
                Vector2 arenaArea = new Vector2(225f, 225f);
                npc.Infernum().arenaRectangle = Utils.CenteredRectangle(target.Center, arenaArea * 16f);
                int left = (int)(npc.Infernum().arenaRectangle.Center().X / 16 - arenaArea.X * 0.5f);
                int right = (int)(npc.Infernum().arenaRectangle.Center().X / 16 + arenaArea.X * 0.5f);
                int top = (int)(npc.Infernum().arenaRectangle.Center().Y / 16 - arenaArea.Y * 0.5f);
                int bottom = (int)(npc.Infernum().arenaRectangle.Center().Y / 16 + arenaArea.Y * 0.5f);
                int arenaTileType = ModContent.TileType<ArenaTile>();

                for (int i = left; i <= right; i++)
                {
                    for (int j = top; j <= bottom; j++)
                    {
                        if (!WorldGen.InWorld(i, j))
                            continue;

                        // Create arena tiles.
                        if ((i == left || i == right || j == top || j == bottom) && !Main.tile[i, j].active())
                        {
                            Main.tile[i, j].type = (ushort)arenaTileType;
                            Main.tile[i, j].active(true);
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendTileSquare(-1, i, j, 1, TileChangeType.None);
                            else
                                WorldGen.SquareTileFrame(i, j, true);
                        }

                        // Erase old arena tiles.
                        else if (CalamityUtils.ParanoidTileRetrieval(i, j).type == arenaTileType)
                            Main.tile[i, j].active(false);
                    }
                }

                attackTextDelay = 180f;
                npc.localAI[1] = 1f;
            }

            // Handle text attack delays. These are used specifically for things like dialog.
            if (attackTextDelay > 0f)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                DoBehavior_HandleattackTextDelaysAndText(npc, target, ref textState, ref attackTextDelay);

                if (textState == 0f && attackTextDelay == 2f)
                    initialChargeupTime = 240f;

                attackTextDelay--;
                return false;
            }

            if (initialChargeupTime > 0f)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                DoBehavior_HandleInitialChargeup(npc, target, ref initialChargeupTime);
                initialChargeupTime--;
                return false;
            }

            // Hover to the side of the target and watch if Sepulcher is present.
            if (sepulcherIsPresent)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;

                float hoverSpeed = 31f;
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 375f;
                hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
                npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                return false;
            }

            // Handle text and phase triggers.
            if (!sepulcherIsPresent && textState == 1f)
			{
                textState++;
                attackTextDelay = 180f;
                npc.netUpdate = true;
			}
            if (currentPhase == 0f && lifeRatio < Phase2LifeRatio)
			{
                attackTimer = 0f;
                attackCycleIndex = 0f;
                currentPhase++;
                textState++;
                attackTextDelay = 300f;
                npc.netUpdate = true;
			}

            switch (CurrentAttack(npc))
			{
                case SCalAttackType.AcceleratingRedirectingSkulls:
                    npc.damage = 0;
                    DoBehavior_AcceleratingRedirectingSkulls(npc, target, ref attackTimer);
                    break;
                case SCalAttackType.MagicChargeBlasts:
                    DoBehavior_MagicChargeBlasts(npc, target, ref attackTimer);
                    break;
                case SCalAttackType.DarkMagicFireballFan:
                    npc.damage = 0;
                    DoBehavior_DarkMagicFireballFan(npc, target, ref attackTimer);
                    break;
                case SCalAttackType.SwervingBlasts:
                    npc.damage = 0;
                    DoBehavior_SwervingBlasts(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HandleattackTextDelaysAndText(NPC npc, Player target, ref float textState, ref float attackTextDelay)
        {
            // Go to the next text state for later once the delay concludes.
            if (attackTextDelay == 1f)
                textState++;

            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            switch ((int)textState)
            {
                // Start of battle.
                case 0:
                    if (attackTextDelay == 120f)
                        Main.NewText("...So it's you.", Color.Orange);

                    if (attackTextDelay == 20f)
                        Main.NewText("After all you've done, I will make you suffer.", Color.Orange);
                    break;

                // After Sepulcher.
                case 2:
                    if (attackTextDelay == 100f)
                        Main.NewText("...You're still alive?", Color.Orange);
                    break;

                // Phase 2.
                case 4:
                    if (attackTextDelay == 240f)
                        Main.NewText("The powers you wield. The strength you've amassed.", Color.Orange);

                    if (attackTextDelay == 150f)
                        Main.NewText("They will not stop me.", Color.Orange);

                    // Summon the shadow demon.
                    if (attackTextDelay == 75f)
					{
                        Vector2 demonSpawnPosition = npc.Center + Main.rand.NextVector2Circular(150f, 75f);
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/BrimstoneMonsterSpawn"), demonSpawnPosition);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                            NPC.NewNPC((int)demonSpawnPosition.X, (int)demonSpawnPosition.Y, ModContent.NPCType<ShadowDemon>(), npc.whoAmI);
					}

                    InfernumMode.BlackFade = Utils.InverseLerp(300f, 125f, attackTextDelay, true) * 0.6f;
                    break;
            }
        }

        public static void DoBehavior_HandleInitialChargeup(NPC npc, Player target, ref float initialChargeupTime)
        {
            // Slow down and look at the target.
            npc.velocity *= 0.95f;
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Charge up power.
            Vector2 dustSpawnPosition = npc.Center + Main.rand.NextVector2Unit(40f, 60f);
            Vector2 dustSpawnVelocity = (npc.Center - dustSpawnPosition) * 0.08f;

            Dust magic = Dust.NewDustPerfect(dustSpawnPosition, 267, dustSpawnVelocity);
            magic.color = Color.Lerp(Color.Red, Color.Magenta, Main.rand.NextFloat(0.4f));
            magic.scale *= Main.rand.NextFloat(0.9f, 1.2f);
            magic.noGravity = true;

            if (initialChargeupTime % 40f == 39f)
            {
                for (int i = 0; i < 6; i++)
                {
                    dustSpawnPosition = npc.Center + Main.rand.NextVector2Unit(300f, 380f);
                    dustSpawnVelocity = (npc.Center - dustSpawnPosition) * 0.08f;

                    magic = Dust.NewDustPerfect(dustSpawnPosition, 264, dustSpawnVelocity);
                    magic.color = Color.Lerp(Color.Red, Color.Magenta, Main.rand.NextFloat(0.4f));
                    magic.color.A = 127;
                    magic.noLight = true;
                    magic.scale *= Main.rand.NextFloat(1f, 1.3f);
                    magic.noGravity = true;
                }
            }

            // Summon spirits from below the player that congregate above their head.
            // They transform into sepulcher after a certain amount of time has passed.
            if (initialChargeupTime % 9f == 8f && initialChargeupTime >= 45f)
            {
                Main.PlaySound(SoundID.NPCHit36, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 soulSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 800f, 1000f);
                    Vector2 soulVelocity = -Vector2.UnitY.RotatedByRandom(0.72f) * Main.rand.NextFloat(6f, 10.5f);
                    int spirit = Utilities.NewProjectileBetter(soulSpawnPosition, soulVelocity, ModContent.ProjectileType<SepulcherSpirit>(), 0, 0f);

                    if (Main.projectile.IndexInRange(spirit))
                    {
                        Main.projectile[spirit].ai[0] = initialChargeupTime;
                        Main.projectile[spirit].localAI[0] = Main.rand.NextFloat(0.92f, 1.08f) % 1f;
                        Main.projectile[spirit].owner = target.whoAmI;
                    }
                }
            }

            // Create some dust to accompany the spirits.
            Vector2 sepulcherSpawnPosition = target.Center + SepulcherSpawnOffset;
            for (int i = 0; i < 5; i++)
            {
                Vector2 dustSpawnDirection = Main.rand.NextVector2Unit();
                Vector2 dustSpawnOffset = dustSpawnDirection.RotatedBy(-MathHelper.PiOver2) * Main.rand.NextFloat(50f);

                magic = Dust.NewDustPerfect(sepulcherSpawnPosition + dustSpawnOffset, 267);
                magic.scale = Main.rand.NextFloat(1f, 1.5f);
                magic.color = Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat());
                magic.velocity = dustSpawnDirection * Main.rand.NextFloat(10f);
                magic.noGravity = true;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && initialChargeupTime == 1f)
            {
                int sepulcher = NPC.NewNPC((int)sepulcherSpawnPosition.X, (int)sepulcherSpawnPosition.Y, ModContent.NPCType<SCalWormHead>(), 1);
                if (Main.npc.IndexInRange(sepulcher))
                {
                    Main.npc[sepulcher].velocity = -Vector2.UnitY * 11f;
                    CalamityUtils.BossAwakenMessage(sepulcher);
                }
            }
        }

        public static void DoBehavior_AcceleratingRedirectingSkulls(NPC npc, Player target, ref float attackTimer)
        {
            int attackCycleCount = 2;
            int hoverTime = 210;
            float hoverHorizontalOffset = 600f;
            float hoverSpeed = 28f;
            float initialFlameSpeed = 14.5f;
            float flameAngularVariance = 1.08f;
            int flameReleaseRate = 9;
            int flameReleaseTime = 180;

            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Attempt to hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverHorizontalOffset;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.
            // This is done to ensure that the attack begins once the boss is close to the target.
            if (attackSubstate == 0f && (attackTimer > hoverTime || npc.WithinRange(hoverDestination, 110f)))
            {
                attackSubstate = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Release skulls.
            if (attackSubstate == 1f)
            {
                if (attackTimer % flameReleaseRate == flameReleaseRate - 1f && attackTimer % 90f > 20f && attackTimer > 45f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int dartDamage = 540;
                        float idealDirection = npc.AngleTo(target.Center);
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center + target.velocity * 42f, -Vector2.UnitY).RotatedByRandom(flameAngularVariance) * initialFlameSpeed;

                        int cinder = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AdjustingDarkMagicSkull>(), dartDamage, 0f);
                        if (Main.projectile.IndexInRange(cinder))
                            Main.projectile[cinder].ai[0] = idealDirection;
                    }
                }

                if (attackTimer > flameReleaseTime)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackCycleCounter++;

                    if (attackCycleCounter > attackCycleCount)
                        SelectNewAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_MagicChargeBlasts(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 3;
            int fadeoutTime = 30;
            int chargeTime = 15;
            Vector2 hoverDestination = target.Center;
            hoverDestination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;
            hoverDestination.Y += (target.Center.Y < npc.Center.Y).ToDirectionInt() * 375f;

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            switch ((int)attackState)
            {
                // Hover in place.
                case 0:
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 31f, 0.7f);
                    npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

                    if (npc.WithinRange(hoverDestination, 45f))
                    {
                        attackState = 1f;
                        attackTimer = 0f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * 35f;
                        npc.netUpdate = true;

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SCalSounds/SCalDash"), npc.Center);
                    }
                    break;

                // Fade away for a moment and create explosions.
                case 1:
                    if (attackTimer < fadeoutTime)
                        npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.065f, 0f, 1f);
                    if (attackTimer > fadeoutTime + chargeTime)
                    {
                        npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.065f, 0f, 1f);
                        npc.velocity *= 0.825f;
                        npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) - MathHelper.PiOver2, 0.25f);
                    }

                    // Create brimstone bombs.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == fadeoutTime + chargeTime + 10f)
					{
                        for (int i = 0; i < 4; i++)
						{
                            Vector2 bombSpawnPosition = (npc.Center + target.Center) * 0.5f + Main.rand.NextVector2Circular(180f, 180f);
                            Utilities.NewProjectileBetter(bombSpawnPosition, Vector2.Zero, ModContent.ProjectileType<DarkMagicBomb>(), 550, 0f);
						}
					}

                    if (attackTimer > fadeoutTime + chargeTime + 25f)
					{
                        if (chargeCounter < chargeCount - 1f)
                        {
                            attackTimer = 0f;
                            attackState = 0f;
                            chargeCounter++;
                        }
                        else
                            SelectNewAttack(npc);
					}

                    if (npc.Opacity < 0.4f)
                        npc.damage = 0;
                    break;
            }
        }

        public static void DoBehavior_DarkMagicFireballFan(NPC npc, Player target, ref float attackTimer)
        {
            int attackCycleTime = 110;
            int attackDelay = 50;
            int shootTime = 50;
            float wrappedAttackTimer = attackTimer % attackCycleTime;
            bool aboutToFire = wrappedAttackTimer > attackDelay - 15f && wrappedAttackTimer < attackDelay + shootTime;
            bool firing = wrappedAttackTimer > attackDelay && aboutToFire && attackTimer % 3f == 0f;
            float hoverSpeed = 45f;
            if (aboutToFire)
                hoverSpeed *= 0.15f;

            ref float rotationDirection = ref npc.Infernum().ExtraAI[0];
            ref float rotationIncrementCounter = ref npc.Infernum().ExtraAI[1];
            ref float shootDirection = ref npc.Infernum().ExtraAI[2];

            // Decide an initial rotation direction.
            if (rotationDirection == 0f)
			{
                rotationDirection = Main.rand.NextBool(2).ToDirectionInt();
                npc.netUpdate = true;
			}

            // Decide the shoot direction.
            if (wrappedAttackTimer == attackDelay - 1f)
			{
                shootDirection = npc.AngleTo(target.Center);
                npc.netUpdate = true;
			}

            float rotationalOffset = rotationDirection * rotationIncrementCounter * MathHelper.PiOver2;

            // Hover near the player.
            // This movement is slowed down prior to and during firing.
            Vector2 hoverDestination = target.Center + rotationalOffset.ToRotationVector2() * (rotationIncrementCounter % 2f == 1f ? 450f : 400f);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed);
            npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.08f);
            npc.SimpleFlyMovement(idealVelocity, hoverSpeed / 50f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Fire the fan.
            if (firing)
			{
                float shootRotationalOffset = MathHelper.Lerp(-1.33f, 1.33f, Utils.InverseLerp(attackDelay, attackDelay + shootTime, wrappedAttackTimer, true));
                Vector2 shootVelocity = (shootRotationalOffset + shootDirection).ToRotationVector2() * 9f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicBurst>(), 540, 0f);

                Main.PlaySound(SoundID.Item73, target.Center);
            }

            // Switch to the next position after an attack cycle.
            if (wrappedAttackTimer == attackCycleTime - 1f)
			{
                rotationIncrementCounter += rotationDirection;
                if (Math.Abs(rotationIncrementCounter) >= 4f)
                    SelectNewAttack(npc);
			}
        }

        public static void DoBehavior_SwervingBlasts(NPC npc, Player target, ref float attackTimer)
        {
            int lightBurstCount = 32;
            int lightBurstShootRate = 2;
            int lightBurstAttackDelay = 160;

            bool inDelay = attackTimer >= 20f + lightBurstCount * lightBurstShootRate && attackTimer < 20 + lightBurstCount * lightBurstShootRate + lightBurstAttackDelay;
            ref float shotCounter = ref npc.Infernum().ExtraAI[0];

            // Teleport above the player.
            if (attackTimer == 15f)
            {
                Vector2 teleportPosition = target.Center - Vector2.UnitY * 270f;
                Dust.QuickDustLine(npc.Center, teleportPosition, 250f, Color.Red);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = Vector2.Zero;
                    npc.Center = teleportPosition;
                    npc.spriteDirection = Main.rand.NextBool(2).ToDirectionInt();
                    npc.netUpdate = true;
                }
            }

            // Release a burst of light.
            if (attackTimer > 15f && !inDelay)
            {
                if (attackTimer > 20f && attackTimer % lightBurstShootRate == lightBurstShootRate - 1f)
                {
                    // Release a burst of dark magic.
                    for (int i = 0; i < 16; i++)
                    {
                        Dust darkMagic = Dust.NewDustPerfect(npc.Center, 264);
                        darkMagic.scale = 0.95f;
                        darkMagic.fadeIn = 0.35f;
                        darkMagic.velocity = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 3.5f;
                        darkMagic.velocity.Y -= 1.8f;
                        darkMagic.color = Color.Red;
                        darkMagic.noLight = true;
                        darkMagic.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.TargetClosest();

                        Vector2 shootVelocity = -Vector2.UnitY.RotatedBy(MathHelper.TwoPi * shotCounter / lightBurstCount) * 12f;
                        Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<SwervingDarkMagicBlast>(), 540, 0f);

                        shotCounter++;
                        npc.netUpdate = true;
                    }
                }
            }

            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            if (attackTimer > lightBurstCount * lightBurstShootRate + lightBurstAttackDelay + 75f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_BrimstoneLightningBlasts(NPC npc, Player target, ref float attackTimer)
        {
            int lightningShootRate = 38;
            Vector2[] potentialHoverPositions = new Vector2[]
            {
                target.Center + new Vector2(480f, 350f),
                target.Center + new Vector2(-480f, 350f),
                target.Center + new Vector2(480f, -350f),
                target.Center + new Vector2(-480f, -350f),
            };
            float minDistance = 1000000f;
            Vector2 hoverPosition = potentialHoverPositions[0];
            for (int i = 0; i < potentialHoverPositions.Length; i++)
			{
                if (npc.Distance(potentialHoverPositions[i]) < minDistance)
				{
                    minDistance = npc.Distance(potentialHoverPositions[i]);
                    hoverPosition = potentialHoverPositions[i];
                }
			}

            hoverPosition.Y += (float)Math.Cos(attackTimer / 20f) * 75f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverPosition) * 27f, 0.8f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            if (attackTimer > 520f)
			{
                if (attackTimer > 580f)
                    SelectNewAttack(npc);
                return;
			}

            if (attackTimer > 75f && attackTimer % 4f == 3f && attackTimer % 90f > 45f)
			{
                if (npc.velocity.Length() > 8f)
                    npc.velocity *= 0.95f;

                Vector2 lightningSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 20f;

                // Release hand electric dust.
                for (int j = 0; j < 2; j++)
                {
                    Dust electricity = Dust.NewDustPerfect(lightningSpawnPosition, 264);
                    electricity.velocity = Vector2.UnitX.RotatedByRandom(0.2f) * npc.spriteDirection * 2.6f;
                    electricity.scale = Main.rand.NextFloat(1.3f, 1.425f);
                    electricity.fadeIn = 0.9f;
                    electricity.color = Color.Red;
                    electricity.noLight = true;
                    electricity.noGravity = true;
                }

                lightningSpawnPosition += Main.rand.NextVector2Circular(18f, 18f);
                Vector2 lightningVelocity = (target.Center - lightningSpawnPosition + target.velocity * 13f).SafeNormalize(Vector2.UnitY) * 1.15f;
                lightningVelocity += Main.rand.NextVector2Circular(0.125f, 0.125f);

                npc.spriteDirection = (lightningVelocity.X > 0f).ToDirectionInt();
                Main.PlaySound(SoundID.Item72, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int lightning = Utilities.NewProjectileBetter(lightningSpawnPosition, lightningVelocity, ModContent.ProjectileType<RedLightning2>(), 540, 0f);
                    if (Main.projectile.IndexInRange(lightning))
                    {
                        Main.projectile[lightning].ai[0] = Main.projectile[lightning].velocity.ToRotation();
                        Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                    }
                }
            }

            // Create lightning from the sky. This has a delay at the start and end of the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % lightningShootRate == lightningShootRate - 1f && attackTimer > 70f)
            {
                Vector2 lightningSpawnPosition = target.Center + new Vector2(Main.rand.NextFloat(300f, 400f) * Main.rand.NextBool(2).ToDirectionInt(), 40f);
                Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.Zero, ModContent.ProjectileType<RedLightningTelegraph>(), 0, 0f);
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0]++;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
