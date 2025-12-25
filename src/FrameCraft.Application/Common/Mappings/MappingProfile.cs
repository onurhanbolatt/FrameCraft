using AutoMapper;
using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Application.Common.Models;

namespace FrameCraft.Application.Common.Mappings;

/// <summary>
/// Ana AutoMapper profile
/// Tüm mapping'ler burada kayıt edilir
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Customer mappings
        CreateCustomerMappings();
    }

    private void CreateCustomerMappings()
    {
        // Entity → DTO (Read)
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        CreateMap<Customer, CustomerListDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // Command → Entity (Create)
        CreateMap<CreateCustomerCommand, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Command → Entity (Update) - manuel map edilecek
        // UpdateCustomerCommand direkt entity'ye map edilmez,
        // handler'da mevcut entity üzerine yazılır
    }
}
