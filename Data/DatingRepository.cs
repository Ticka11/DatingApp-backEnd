using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using DatingApp_backEnd.Helpers;
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

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.Where(p => p.UserId == userId).FirstOrDefaultAsync(p => p.IsMain == true);
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

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(u => u.Photos).OrderByDescending(u => u.LastActive).AsQueryable();
        
            users = users.Where(u => u.Id != userParams.UserId);
            users = users.Where(u => u.Gender != userParams.Gender);

            if(userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                var minDateOfBirth = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDateOfBirth = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(u => u.DateOfBirth >= minDateOfBirth && u.DateOfBirth <= maxDateOfBirth);
            }

            if(!string.IsNullOrEmpty(userParams.OrderBy))            
            {
                switch(userParams.OrderBy)
                {
                    case "created": users = users.OrderByDescending(u => u.Created);
                    break;

                    default: users = users.OrderByDescending(u => u.LastActive);
                    break;
                    
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
            //return true if it's more than zero
            //equal to zero, nothing is saved into our database 

        }
    }
}