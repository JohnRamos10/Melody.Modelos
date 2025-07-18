using Melody.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Melody.API.Inicializador
{
    public class DbInicializador : IDbInicializador
    {
        private readonly AppDbContext _db;
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;


        public DbInicializador(AppDbContext db, UserManager<Usuario> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InicializarAsync()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate(); // Ejecuta las migraciones pendientes
                }
            }
            catch (Exception)
            {
                throw;
            }

            // Datos Iniciales - Crear roles si no existen
            if (!_db.Roles.Any(r => r.Name == DS.Role_Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>(DS.Role_Admin));
                await _roleManager.CreateAsync(new IdentityRole<int>(DS.Role_Artista));
                await _roleManager.CreateAsync(new IdentityRole<int>(DS.Role_UsuarioFree));
                await _roleManager.CreateAsync(new IdentityRole<int>(DS.Role_UsuarioPremium));
            }

            // Crear usuario administrador SOLO si no existe
            if (!_db.Usuarios.Any(u => u.UserName == "johnescanor77@gmail.com"))
            {
                await _userManager.CreateAsync(new Usuario
                {
                    UserName = "johnescanor77@gmail.com",
                    Email = "johnescanor77gmail.com",
                    EmailConfirmed = true,
                    Nombre = "John",
                    Apellido = "Ramos",
                    FechaRegistro = DateTime.Now
                }, "Admin123");

                // Asignar el rol al usuario
                Usuario usuario = _db.Usuarios.Where(u => u.UserName == "johnescanor77@gmail.com").FirstOrDefault();
                if (usuario != null)
                {
                    await _userManager.AddToRoleAsync(usuario, DS.Role_Admin);
                }
            }
        }
    }
}
