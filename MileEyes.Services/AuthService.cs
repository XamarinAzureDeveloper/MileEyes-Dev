﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using MileEyes.PublicModels;
using MileEyes.Services.Models;
using Newtonsoft.Json;

namespace MileEyes.Services
{
    public class AuthService : IAuthService
    {
        public bool Authenticated { get; private set; }

        public void Logout()
        {
            Authenticated = false;
            DatabaseService.Realm.RemoveAll<AuthToken>();
            RestService.Client.DefaultRequestHeaders.Authorization = null;
        }

        public AuthService()
        {
            var authTokens = DatabaseService.Realm.All<AuthToken>();

            if (authTokens.Any())
            {
                var authToken = authTokens.FirstOrDefault();
                RestService.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);

                Authenticated = true;
            }
            else
            {
                Authenticated = false;
            }
        }

        public async Task<AuthResponse> Authenticate(string email, string password)
        {
            var data = new FormUrlEncodedContent(new []
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", email),
                new KeyValuePair<string, string>("password", password),  
            });

            try
            {
                var response = await RestService.Client.PostAsync("token", data);

                if (!response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<AuthResponse>(await response.Content.ReadAsStringAsync());
                    result.Success = false;
                    return result;
                }

                var tokenResult = JsonConvert.DeserializeObject<AuthResponse>(await response.Content.ReadAsStringAsync());


                using (var transaction = DatabaseService.Realm.BeginWrite())
                {
                    DatabaseService.Realm.RemoveAll<AuthToken>();

                    var authToken = DatabaseService.Realm.CreateObject<AuthToken>();

                    authToken.access_token = tokenResult.access_token;
                    authToken.expires = tokenResult.expires;
                    authToken.expires_in = tokenResult.expires_in;
                    authToken.token_type = tokenResult.token_type;
                    authToken.issued = tokenResult.issued;
                    authToken.userName = tokenResult.userName;

                    transaction.Commit();
                }

                RestService.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.access_token);

                Authenticated = true;
                tokenResult.Success = true;

                return tokenResult;
            }
            catch(Exception ex)
            {
                return new AuthResponse()
                {
                    error = ex.Message
                };
            }
        }

        public async Task<RegisterResponse> Register(string firstName, string lastName, string email, string password,
            string confirmPassword,
            string addressPlaceId)
        {
            var data = new StringContent(JsonConvert.SerializeObject(new RegisterBindingModel()
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword,
                PlaceId = addressPlaceId
            }), Encoding.UTF8, "application/json");

            try
            {
                var response = await RestService.Client.PostAsync("api/Account/Register", data);

                if (response.IsSuccessStatusCode) return new RegisterResponse();

                var resultString = await response.Content.ReadAsStringAsync();

                var result =
                    JsonConvert.DeserializeObject<RegisterResponse>(resultString);
                result.Error = true;

                return result;
            }
            catch (Exception ex)
            {
                return new RegisterResponse()
                {
                    Error = true,
                    Message = ex.Message
                };
            }
        }
    }
}