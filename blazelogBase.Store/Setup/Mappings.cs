using AutoMapper;
using blazelogBase.Store.Dtos;
using blazelogBase.Store.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Store.Setup;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CoreUser, UserDto>().ReverseMap();
        CreateMap<UserDto, AuthUserModel>()
            .ForMember(dt => dt.userKey, ex => ex.MapFrom(ex => ex.Id))
            .ForMember(dt=> dt.email,ex=> ex.MapFrom(ex=> ex.Email))
            .ForMember(dt => dt.isadmin, ex => ex.MapFrom(ex => ex.IsAdmin))
            .ForMember(dt => dt.post, ex => ex.MapFrom(ex => ex.Post))
            .ForMember(dt => dt.userID, ex => ex.MapFrom(ex => ex.UserId))
            .ForMember(dt => dt.userName, ex => ex.MapFrom(ex => ex.UserName))
            .ForMember(dt => dt.division, ex => ex.MapFrom(ex => ex.Division))
            .ForMember(dt => dt.level, ex => ex.MapFrom(ex => ex.Level));

    }
}

