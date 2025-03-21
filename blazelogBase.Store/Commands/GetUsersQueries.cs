using AutoMapper;
using AutoMapper.QueryableExtensions;
using blazelogBase.Shared.Models;
using blazelogBase.Shared.Tools;
using blazelogBase.Store.Dtos;
using blazelogBase.Store.Setup;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace blazelogBase.Store.Commands
{

    public record GetUserQuery(string userId) : IRequest<UserDto>;

    public class GetUserQueryHandler: IRequestHandler<GetUserQuery, UserDto>
    {
        private readonly IMapper mapper;
        private readonly IBlazeLogDbContext context;
        public GetUserQueryHandler(IBlazeLogDbContext ctx,IMapper mp)
        {
            context = ctx;
            mapper = mp;
        }
        public async Task<UserDto> Handle(GetUserQuery query,CancellationToken cancellationToken)
        {
            var data = context.CoreUsers.AsQueryable();
            var user = await data.FirstOrDefaultAsync(e => e.UserId == query.userId);
            if(user!=null)
                return mapper.Map<UserDto>(user);
            return null;
        }
        
    }


    public record GetUsersQuery(string AskSearch,int Start=1,int Size=0,params SortDescription[] Sorts): IRequest<List<UserDto>>;


    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<UserDto>>
    {
        private readonly IBlazeLogDbContext context;
        private readonly IMapper mapper;
        public GetUsersQueryHandler(IBlazeLogDbContext ctx,IMapper mp)
        {
            context = ctx;
            mapper = mp;    
        }
        public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var query = context.CoreUsers.AsQueryable();
            var size = request.Size == 0 ? CN.Setting.PageSize: request.Size;
            var start = request.Start == 0 ? CN.Setting.PageStart : request.Start;
            if (request.AskSearch != null)
            {
                query= query.Where(x => x.UserName.Contains(request.AskSearch) || x.Email.Contains(request.AskSearch));
                
            }
            if (request.Sorts!=null && request.Sorts.Length > 0)
            {
                query= query.BuildOrder(request.Sorts).Skip(start).Take(size);
            }

            return await query.ProjectTo<UserDto>(mapper.ConfigurationProvider).ToListAsync(cancellationToken);

            
        }
    }

}
