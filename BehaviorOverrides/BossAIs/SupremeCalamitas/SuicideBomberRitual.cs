using InfernumMode.ILEditingStuff;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SuicideBomberRitual : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 84;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Ritual");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Projectile.Opacity = Utils.GetLerpValue(0f, 60f, Time, true);
            Projectile.scale = Projectile.Opacity;
            Projectile.direction = (Projectile.identity % 2 == 0).ToDirectionInt();
            Projectile.rotation += Projectile.direction * 0.18f;

            Time++;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BrimstoneDemonSummonExplosion>(), 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/SupremeCalamitas/SuicideBomberRitual").Value;
            Texture2D innerCircle = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/SupremeCalamitas/SuicideBomberRitualCircleInner").Value;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.Red, Color.Blue, Projectile.identity / 6f % 1f));
            Color color2 = Projectile.GetAlpha(Color.Lerp(Color.Red, Color.Blue, (Projectile.identity / 6f + 0.27f) % 1f));
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(texture, drawPosition, null, color, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0f);
            Main.spriteBatch.Draw(innerCircle, drawPosition, null, color2, -Projectile.rotation, innerCircle.Size() * 0.5f, Projectile.scale, 0, 0f);
            return false;
        }

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI, List<int> overWiresUI)
        {
            DrawBlackEffectHook.DrawCacheAdditiveLighting.Add(index);
        }
    }
}
