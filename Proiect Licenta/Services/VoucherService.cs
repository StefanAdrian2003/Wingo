using Microsoft.EntityFrameworkCore;
using Proiect_Licenta.Data;
using Proiect_Licenta.Models;

namespace Proiect_Licenta.Services
{
    public class VoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Atribuie un voucher random unui user.
        /// Creează un obiect Voucher în baza de date legat de user.
        /// </summary>
        public async Task<Voucher> AssignRandomVoucherAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // 1. Alegem template-ul folosind noul Random.Shared (mai sigur)
            var templates = VoucherTemplates.Templates;
            var chosen = templates[Random.Shared.Next(templates.Count)];

            // 2. Construim obiectul Voucher
            var voucher = new Voucher
            {
                UserId = user.Id,
                Name = chosen.Name,
                Description = chosen.Description,
                // Generăm un cod scurt de 8 caractere, Uppercase
                Code = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            };

            // 3. Salvăm în DB
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            return voucher;
        }

        /// <summary>
        /// Returnează toate voucherele unui user.
        /// </summary>
        public async Task<List<Voucher>> GetUserVouchersAsync(string userId)
        {
            return await _context.Vouchers
                .Where(v => v.UserId == userId)
                .ToListAsync();
        }
    }
}
