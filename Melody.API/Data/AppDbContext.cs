using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Melody.Modelos;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
    {
    }

    public DbSet<Album> Albums { get; set; } = default!;
    public DbSet<Cancion> Canciones { get; set; } = default!;
    public DbSet<Genero> Generos { get; set; } = default!;
    public DbSet<Pago> Pagos { get; set; } = default!;
    public DbSet<Plan> Planes { get; set; } = default!;
    public DbSet<Playlist> Playlists { get; set; } = default!;
    public DbSet<PlaylistCancion> PlaylistsCanciones { get; set; } = default!;
    public DbSet<Seguimiento> Seguimientos { get; set; } = default!;
    public DbSet<Suscripcion> Suscripciones { get; set; } = default!;
    public DbSet<Usuario> Usuarios { get; set; } = default!;
    public DbSet<Artista> Artistas { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Relación uno a uno: Artista tiene un Usuario
        builder.Entity<Artista>()
            .HasOne(a => a.Usuario)
            .WithOne(u => u.Artista)
            .HasForeignKey<Artista>(a => a.UsuarioId)
            .IsRequired();

        // Relación muchos a uno: Usuario sigue a varios artistas
        builder.Entity<Seguimiento>()
            .HasOne(s => s.Usuario)
            .WithMany(u => u.Seguimientos)
            .HasForeignKey(s => s.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict); // Evita cascada para evitar eliminaciones no deseadas

        // Relación muchos a uno: Artista tiene varios seguidores
        builder.Entity<Seguimiento>()
            .HasOne(s => s.Artista)
            .WithMany(a => a.Seguidores)
            .HasForeignKey(s => s.ArtistaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configuración para PlaylistCancion evitar cascadas múltiples
        builder.Entity<PlaylistCancion>()
            .HasOne(pc => pc.Playlist)
            .WithMany(p => p.PlaylistsCanciones)
            .HasForeignKey(pc => pc.PlaylistId)
            .OnDelete(DeleteBehavior.Restrict); // Cambia cascada a Restrict aquí

        builder.Entity<PlaylistCancion>()
            .HasOne(pc => pc.Cancion)
            .WithMany(c => c.PlaylistsCanciones)
            .HasForeignKey(pc => pc.CancionId)
            .OnDelete(DeleteBehavior.Cascade); // Cascada en la otra FK puede quedar

        // Semillas de roles
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "admin", NormalizedName = "ADMIN" },
            new IdentityRole<int> { Id = 2, Name = "artista", NormalizedName = "ARTISTA" },
            new IdentityRole<int> { Id = 3, Name = "userfree", NormalizedName = "USERFREE" },
            new IdentityRole<int> { Id = 4, Name = "userpremium", NormalizedName = "USERPREMIUM" }
        );
    }
}
