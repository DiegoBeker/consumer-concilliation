using System.ComponentModel.DataAnnotations;

namespace concilliation_consumer.Dtos;

public class ConcilliationDTO
{
  [DataType(DataType.Date)]
  [Required]
  public required DateTime Date { get; set; }

  [Required]
  public required string FilePath { get; set; }

  [Required]
  public required string Postback { get; set; }
}

public class ConcilliationMessageDTO(ConcilliationDTO concilliation, long paymentProviderId)
{

  public long PaymentProviderId { get; } = paymentProviderId;
  public ConcilliationDTO Concilliation { get; } = concilliation;
}