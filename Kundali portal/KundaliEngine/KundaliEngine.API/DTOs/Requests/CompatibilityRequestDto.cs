using System;

namespace KundaliEngine.API.DTOs.Requests
{
    public class CompatibilityRequestDto
    {
        public GenerateKundaliRequest PersonA { get; set; } = new GenerateKundaliRequest();
        public GenerateKundaliRequest PersonB { get; set; } = new GenerateKundaliRequest();
    }
}