using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.KingSlime
{
	public class Shuriken : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shuriken");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 240;
		}

        public override void AI()
        {
            projectile.alpha = Utils.Clamp(projectile.alpha - 25, 0, 255);
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.4f;
            projectile.tileCollide = projectile.timeLeft < 90;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D shurikenTexture = Main.projectileTexture[projectile.type];
            float pulseOutwardness = MathHelper.Lerp(4f, 8f, (float)Math.Cos(Main.GlobalTime * 2.5f) * 0.5f + 0.5f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * pulseOutwardness;
                Vector2 drawPosition = projectile.Center - Main.screenPosition + drawOffset;
                Color afterimageColor = Color.Lerp(Color.DarkGray, Color.Black, 0.66f) * projectile.Opacity * 0.5f;
                spriteBatch.Draw(shurikenTexture, drawPosition, null, afterimageColor, projectile.rotation, shurikenTexture.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }
            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, lightColor, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override void Kill(int timeLeft) => Collision.HitTiles(projectile.position, projectile.velocity, 24, 24);
    }
}
