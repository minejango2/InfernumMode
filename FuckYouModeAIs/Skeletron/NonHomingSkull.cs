using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Skeletron
{
    public class NonHomingSkull : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Skull");
            Main.projFrames[projectile.type] = 3;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 26;
            projectile.height = 28;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 300;
            cooldownSlot = 1;
        }
        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
			{
                projectile.frame = Main.rand.Next(Main.projFrames[projectile.type]);
                projectile.localAI[0] = 1f;
			}

            projectile.velocity *= 1.026f;
            projectile.rotation = projectile.velocity.ToRotation();
            projectile.spriteDirection = (System.Math.Cos(projectile.rotation) > 0f).ToDirectionInt();
            if (projectile.spriteDirection == -1)
                projectile.rotation += MathHelper.Pi;

            for (int i = 0; i < 2; i++)
			{
                Dust magic = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 264);
                magic.velocity = -projectile.velocity.RotatedByRandom(0.53f) * 0.15f;
                magic.scale = Main.rand.NextFloat(0.45f, 0.7f);
                magic.fadeIn = 0.6f;
                magic.noLight = true;
                magic.noGravity = true;
			}

            Lighting.AddLight(projectile.Center, Color.White.ToVector3());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Color drawColor = Color.Purple;
            drawColor.A = 0;

            CalamityGlobalProjectile.DrawCenteredAndAfterimage(projectile, drawColor, ProjectileID.Sets.TrailingMode[projectile.type], 2);
            return true;
        }
    }
}
