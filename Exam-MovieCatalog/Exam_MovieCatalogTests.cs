using System;
using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using Exam_MovieCatalog.DTOs;



namespace Exam_MovieCatalog
{
    [TestFixture]
    public class Exam_MovieCatalogTests
    {
        private RestClient client;
        private static string lastCreatedMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJkNTBjOTcwYi0zYjA3LTQ1MzYtYmY5NC1lOGY3MTMzNTY5OTEiLCJpYXQiOiIwNC8xOC8yMDI2IDA1OjU5OjQ1IiwiVXNlcklkIjoiZDIzOTY3YzYtZTE4MC00MWZhLTYyMGQtMDhkZTc2OTcxYWI5IiwiRW1haWwiOiJhbl9hbGV4QGFidi5iZyIsIlVzZXJOYW1lIjoiYW5hbGV4X3N1IiwiZXhwIjoxNzc2NTEzNTg1LCJpc3MiOiJNb3ZpZUNhdGFsb2dfQXBwX1NvZnRVbmkiLCJhdWQiOiJNb3ZpZUNhdGFsb2dfV2ViQVBJX1NvZnRVbmkifQ.28iCpIfTjxBIWZwYSo5gmz_UnUmwwFsGqG4o3d_CiHY";

        private const string LoginEmail = "an_alex@abv.bg";
        private const string LoginPassword = "analex24";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient tempClient = new RestClient(BaseUrl);
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateANewMovieWithRequiredFields_ShouldReturnSuccess()
        {
            MovieDTO movieData = new MovieDTO
            {
                Title = "The Lord of the Rings: The Fellowship of the Ring",
                Description = "This is the first movie in the Lord of the Rings trilogy."
            };
            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            RestResponse response = this.client.Execute(request);

            ApiResponseDTO createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Expected status code 200 OK.");
            //Assert.That(createResponse, Is.Not.Empty);
            Assert.IsNotEmpty(response.Content, "Response content should not be empty.");
            Assert.IsNotNull(response.Content, "Movie data should not be null.");
            Assert.That(createResponse.Movie.Id, Is.Not.Empty); 
            Assert.That(createResponse.Movie.Id, Is.Not.Null);
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));

            lastCreatedMovieId = createResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditLastCreatedMovie_ShouldReturnSuccess()
        {
            MovieDTO editMovieData = new MovieDTO
            {
                Title = "The Lord of the Rings: 1st movie",
                Description = "The Fellowship of the Ring",
            };

            RestRequest request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            request.AddJsonBody(editMovieData);

            RestResponse response = this.client.Execute(request);

            ApiResponseDTO editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));

        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest("/api/Catalog/All", Method.Get);
            RestResponse response = this.client.Execute(request);

            List<ApiResponseDTO> allMoviesResponse = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(allMoviesResponse, Is.Not.Empty);
            Assert.That(allMoviesResponse, Is.Not.Null);
            Assert.That(allMoviesResponse.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Order(4)]
        [Test]
        public void DeleteLastCreatedMovie_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", lastCreatedMovieId);
            
            RestResponse response = this.client.Execute(request);

            ApiResponseDTO deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovieWithoutRequiredFields_ShouldReturnBadRequest()
        {
            MovieDTO movieData = new MovieDTO
            {
                Title = string.Empty,
                Description = string.Empty
            };
            
            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            RestResponse response = this.client.Execute(request);

            ApiResponseDTO createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistentMovie_ShouldReturnBadRequest()
        {
            string nonExistentMovieId = "00000000-0000-0000-0000-000000000000";

            MovieDTO editMovieData = new MovieDTO
            {
                Title = "Non-existent movie",
                Description = "his movie does not exist",
            };

            RestRequest request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistentMovieId);
            request.AddJsonBody(editMovieData);

            RestResponse response = this.client.Execute(request);

            ApiResponseDTO editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(editResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistentMovie_ShouldReturnBadRequest()
        {
            string nonExistentMovieId = "00000000-0000-0000-0000-000000000000";

            MovieDTO deleteMovieData = new MovieDTO
            {
                Title = "Non-existent movie",
                Description = "his movie does not exist",
            };
            RestRequest request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistentMovieId);

            RestResponse response = this.client.Execute(request);

            ApiResponseDTO deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(deleteResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]

            public void TearDown()
            {
                this.client.Dispose();
            }
        }
    
}