using InfernumMode.Assets.Effects;
using InfernumMode.Common.Graphics.Interfaces;
using InfernumMode.Common.Graphics.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Twins
{
    public class HomingCursedFlameBurst : ModProjectile, IPixelPrimitiveDrawer
    {
        public PrimitiveTrailCopy FireDrawer;

        public const int HomeTime = 90;

        public const int Lifetime = 360;

        public const int TimeBeforeSwirl = 170;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flame");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 34;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.Opacity = MathF.Sin(Projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * 5f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.Green.ToVector3() * 1.45f);

            if (Projectile.timeLeft >= Lifetime - HomeTime)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                float idealAngle = Projectile.AngleTo(target.Center);
                Projectile.velocity = Projectile.velocity.ToRotation().AngleLerp(idealAngle, 0.008f).ToRotationVector2() * Projectile.velocity.Length();
            }

            // Make an incomplete arc after a certain amount of time has passed.
            if (Projectile.timeLeft <= Lifetime - TimeBeforeSwirl)
                Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.TwoPi * 0.66f / (Lifetime - TimeBeforeSwirl));

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                Dust fire = Dust.NewDustPerfect(Projectile.Center, 27);
                fire.velocity = Main.rand.NextVector2CircularEdge(3f, 3f);
                fire.scale *= 1.1f;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                float _ = 0f;
                Vector2 top = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 bottom = Projectile.oldPos[i + 1] + Projectile.Size * 0.5f;

                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), top, bottom, 11f, ref _))
                    return true;
            }
            return false;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = MathF.Pow(Utils.GetLerpValue(0f, 0.27f, completionRatio, true), 0.4f) * Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(3f, Projectile.width, squeezeInterpolant) * Projectile.Opacity;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.GreenYellow, Color.White, MathF.Pow(completionRatio, 2f));
            color *= 1f - 0.67f * MathF.Pow(completionRatio, 3f);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawPixelPrimitives(SpriteBatch spriteBatch)
        {
            FireDrawer ??= new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, InfernumEffectsRegistry.FireVertexShader);

            InfernumEffectsRegistry.FireVertexShader.UseSaturation(Projectile.velocity.Length() / 13f);
            InfernumEffectsRegistry.FireVertexShader.UseImage1("Images/Misc/Perlin");
            FireDrawer.DrawPixelated(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 32);
        }
    }
}
