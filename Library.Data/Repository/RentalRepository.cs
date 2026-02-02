using Library.Business.Interfaces.IRepository;
using Library.Business.Models;
using Library.Business.Pagination;
using Library.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Library.Data.Repository
{
    public class RentalRepository : Repository<Rentals>, IRentalRepository
    {
        public RentalRepository(DataContext context) : base(context)
        {
        }

        public async Task<PagedBaseResponse<Rentals>> GetAllRentalsPaged(FilterDb request)
        {
            var rentals = _context.Rentals.Include(r => r.User).Include(r => r.Book).AsQueryable();
            if (request.FilterValue != null)
            {
                var search = request.FilterValue.ToLower();
                rentals = rentals.Where(
                    r => r.Id.ToString().Contains(search) ||
                    r.RentalDate.ToString().Contains(search) ||
                    r.ForecastDate.ToString().Contains(search) ||
                    r.ReturnDate.ToString().Contains(search) ||
                    r.Status.ToLower().Contains(search) ||
                    r.BookId.ToString().Contains(search) ||
                    r.Book.Name.ToLower().Contains(search) ||
                    r.UserId.ToString().Contains(search) ||
                    r.User.Name.ToLower().Contains(search)
                );
            }
            return await PagedBaseResponseHelper.GetResponseAsync<PagedBaseResponse<Rentals>, Rentals>(rentals, request);
        }
        public async Task<List<Rentals>> GetAllRentals()
        {
            return await _context.Rentals.Include(r => r.Book).ToListAsync();
        }

        public async Task<Rentals> GetRentalById(int rentalId)
        {
            return await _context.Rentals.AsNoTracking().Include(r => r.User).Include(r => r.Book).FirstOrDefaultAsync(r => r.Id == rentalId);
        }

        public async Task<Rentals> GetRentalByIdNoIncludes(int rentalId)
        {
            return await _context.Rentals.AsNoTracking().FirstOrDefaultAsync(r => r.Id == rentalId);
        }
    }
}