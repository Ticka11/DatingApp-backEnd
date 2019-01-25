using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;
        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            // we do not add async, because we don't execute queries on database
            // we only add data
            // same for deletion
            // first, its saved into memory, until we save it to the database 
            // reason why we dont add async
            _context.Add(entity);   
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Photo> GetPhoto(int id)
        {
             var photo = await _context.Photos.FirstOrDefaultAsync(p => p.Id == id);
             return photo;

        }

        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.Include(x => x.Photos)
                                    .FirstOrDefaultAsync(x => x.Id == id);
            return user;
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            var users = await _context.Users.Include(p => p.Photos).ToListAsync();
            return users;
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
            //return true if it's more than zero
            //equal to zero, nothing is saved into our database 

        }
    }
}