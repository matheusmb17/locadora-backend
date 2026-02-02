#pragma warning disable CS8604
using AutoMapper;
using Library.Business.Interfaces.IRepository;
using Library.Business.Interfaces.IServices;
using Library.Business.Models;
using Library.Business.Models.Dtos;
using Library.Business.Models.Dtos.Rental;
using Library.Business.Models.Dtos.Validations;
using Library.Business.Pagination;

namespace Library.Business.Services
{
    public class RentalsService : IRentalsService
    {
        private readonly IRentalRepository _rentalRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public RentalsService(IRentalRepository repo, IBookRepository bookRepo, IUserRepository userRepo, IMapper mapper)
        {
            _rentalRepository = repo;
            _bookRepository = bookRepo;
            _userRepository = userRepo;
            _mapper = mapper;
        }

        public async Task<ResultService<List<RentalDto>>> GetAll(FilterDb filterDb)
        {
            var rentals = await _rentalRepository.GetAllRentalsPaged(filterDb);
            var result = new PagedBaseResponseDto<RentalDto>(rentals.TotalRegisters, rentals.TotalPages, rentals.PageNumber, _mapper.Map<List<RentalDto>>(rentals.Data));

            if (result.Data.Count == 0) return ResultService.NotFound<List<RentalDto>>("Nenhum registro encontrado.");

            return ResultService.OkPaged(result.Data, result.TotalRegisters, result.PageNumber, result.TotalPages);
        }

        public async Task<ResultService<RentalDto>> GetById(int id)
        {
            var rental = await _rentalRepository.GetRentalById(id);
            if (rental == null) return ResultService.NotFound<RentalDto>("Aluguel não encontrado!");

            var rentalDto = _mapper.Map<RentalDto>(rental);
            return ResultService.Ok(rentalDto);
        }

        public async Task<ResultService> Create(CreateRentalDto model)
        {
            var validation = new RentalDtoValidator().Validate(model);
            if (!validation.IsValid) return ResultService.BadRequest(validation);
            
            if (!_bookRepository.Search(b => b.Id == model.BookId).Result.Any()) return ResultService.NotFound<CreateRentalDto>("Livro não encontrado!");

            if (!_userRepository.Search(u => u.Id == model.UserId).Result.Any()) return ResultService.NotFound<CreateRentalDto>("Usuário não encontrado!");

            if (model.RentalDate != DateTime.Now.Date) return ResultService.BadRequest("Data de aluguel não pode ser diferente da data de Hoje!");

            if (model.ForecastDate.Subtract(model.RentalDate).Days > 30) return ResultService.BadRequest("Prazo do aluguel não pode ser superior a 30 dias!");

            if (model.ForecastDate < model.RentalDate) return ResultService.BadRequest("Data de Previsão não pode ser anterior à Data do Aluguel!");

            if (_rentalRepository.Search(r => r.UserId == model.UserId && r.BookId == model.BookId && r.ReturnDate == null).Result.Any()) return ResultService.BadRequest("Usuário já possui aluguel desse livro!");

            var book = await _bookRepository.GetBookById(model.BookId);
            book.Quantity--;
            book.Rented++;
            if (book.Quantity < 0) return ResultService.BadRequest("Livro com estoque esgotado.");
            await _bookRepository.Update(book);

            var rental = _mapper.Map<Rentals>(model);
            rental.Status = "Pendente";
            await _rentalRepository.Add(rental);
            return ResultService.Created("Aluguel adicionado com êxito.");
        }

        public async Task<ResultService> Update(UpdateRentalDto model)
        {
            var result = await _rentalRepository.GetRentalByIdNoIncludes(model.Id);
            if (result == null) return ResultService.NotFound<UpdateRentalDto>("Aluguel não encontrado!");

            var rental = _mapper.Map(model, result);

            var validation = new UpdateRentalDtoValidator().Validate(model);
            if (!validation.IsValid) return ResultService.BadRequest(validation);

            if (rental.ReturnDate.Value.Date != DateTime.Now.Date) return ResultService.BadRequest("Data de devolução não pode ser diferente da data de Hoje!");
            
            rental.Status = rental.ForecastDate < rental.ReturnDate ? "Atrasado" : "No prazo";

            var book = await _bookRepository.GetBookById(rental.BookId);
            book.Quantity++;
            book.Rented--;

            await _bookRepository.Update(book);
            await _rentalRepository.Update(rental);
            return ResultService.Ok("Devolução realizada com êxito!");
        }

        public async Task<ResultService> Delete(int id)
        {
            var rental = await _rentalRepository.GetRentalByIdNoIncludes(id);
            if (rental == null) return ResultService.NotFound<RentalDto>("Aluguel não encontrado!");

            await _rentalRepository.Delete(id);

            var book = await _bookRepository.GetBookById(rental.BookId);
            book.Quantity++;
            book.Rented--;
            await _bookRepository.Update(book);
            return ResultService.Ok("Aluguel deletado com êxito!");
        }
    }
}
