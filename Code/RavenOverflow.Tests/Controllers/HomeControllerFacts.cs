﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using RavenOverflow.Core.Entities;
using RavenOverflow.FakeData;
using RavenOverflow.Web.AutoMapper;
using RavenOverflow.Web.Controllers;
using RavenOverflow.Web.Models.ViewModels;
using RavenOverflow.Web.RavenDb.Indexes;
using Xunit;

namespace RavenOverflow.Tests.Controllers
{
    // ReSharper disable InconsistentNaming

    public class HomeControllerFacts : RavenDbFactBase
    {
        public HomeControllerFacts()
        {
            // WebSite requires AutoMapper mappings.
            AutoMapperBootstrapper.ConfigureMappings();
            Mapper.AssertConfigurationIsValid();
        }

        [Fact]
        public void GivenSomeQuestions_Index_ReturnsTheMostRecentQuestions()
        {
            IndexesToExecute = new List<Type> {typeof (Questions_Search), typeof (RecentPopularTags)};

            // Arrange.
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Index(null, null) as ViewResult;

            // Assert.
            Assert.NotNull(result);

            var model = result.Model as IndexViewModel;
            Assert.NotNull(model);
            Assert.NotNull(model.QuestionListViewModel);
            Assert.NotNull(model.QuestionListViewModel.Questions);
            Assert.Equal(20, model.QuestionListViewModel.Questions.Count);

            // Make sure all the items are ordered correctly.
            DateTime? previousQuestion = null;
            foreach (QuestionWithDisplayName question in model.QuestionListViewModel.Questions)
            {
                if (previousQuestion.HasValue)
                {
                    Assert.True(previousQuestion.Value >= question.CreatedOn);
                }

                previousQuestion = question.CreatedOn;
            }

            // Now lets test that our fixed questions come back correctly.
            List<Question> fixedQuestions = FakeQuestions.CreateFakeQuestions(null, 5).ToList();
            for (int i = 0; i < 5; i++)
            {
                // Can Assert anything except
                // * Id (these new fakes haven't been Stored)
                // * CreatedByUserId - this is randomized when fakes are created.
                // * CreatedOn - these fakes were made AFTER the Stored data.
                // ASSUMPTION: the first 5 fixed questions are the first 5 documents in the Document Store.
                QuestionWithDisplayName question = model.QuestionListViewModel.Questions[i];
                Assert.Equal(fixedQuestions[i].Subject, question.Subject);
                Assert.Equal(fixedQuestions[i].Content, question.Content);
                Assert.Equal(fixedQuestions[i].NumberOfViews, question.NumberOfViews);
                Assert.Equal(fixedQuestions[i].Vote.DownVoteCount, question.Vote.DownVoteCount);
                Assert.Equal(fixedQuestions[i].Vote.FavoriteCount, question.Vote.FavoriteCount);
                Assert.Equal(fixedQuestions[i].Vote.UpVoteCount, question.Vote.UpVoteCount);
            }
        }

        [Fact]
        public void GivenSomeQuestions_Index_ReturnsTheMostRecentPopularTagsInTheLast30Days()
        {
            IndexesToExecute = new List<Type> {typeof (Questions_Search), typeof (RecentPopularTags)};

            // Arrange.
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Index(null, null) as ViewResult;

            // Assert.
            Assert.NotNull(result);

            var model = result.Model as IndexViewModel;
            Assert.NotNull(model);

            Assert.NotNull(model.RecentPopularTags);
            Assert.True(model.RecentPopularTags.Count > 0);

            // Make sure all the items are ordered correctly.
            int? previousCount = null;
            foreach (var keyValuePair in model.RecentPopularTags)
            {
                if (previousCount.HasValue)
                {
                    Assert.True(previousCount.Value >= keyValuePair.Value);
                }

                previousCount = keyValuePair.Value;
            }
        }

        [Fact]
        public void GivenAnAuthenticatedUserWithSomeFavouriteTags_Index_ReturnsAFavouriteTagsViewModelWithContent()
        {
            IndexesToExecute = new List<Type> {typeof (Questions_Search), typeof (RecentPopularTags)};

            // Arrange.
            // Note: we're faking that a user has authenticated.
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController, displayName: "Pure.Krome");


            // Act.
            var result = homeController.Index(null, null) as ViewResult;

            // Assert.
            Assert.NotNull(result);

            var model = result.Model as IndexViewModel;
            Assert.NotNull(model);

            UserTagListViewModel userFavoriteTagListViewModel = model.UserFavoriteTagListViewModel;
            Assert.NotNull(userFavoriteTagListViewModel);

            Assert.Equal("Favorite Tags", userFavoriteTagListViewModel.Header);
            Assert.Equal("interesting-tags", userFavoriteTagListViewModel.DivId1);
            Assert.Equal("interestingtags", userFavoriteTagListViewModel.DivId2);
            Assert.NotNull(userFavoriteTagListViewModel.Tags);
            Assert.Equal(3, userFavoriteTagListViewModel.Tags.Count);
        }

        [Fact]
        public void GivenNoAuthenticatedUser_Index_ReturnsFavouriteTagsViewModelWithNoTags()
        {
            IndexesToExecute = new List<Type> {typeof (Questions_Search), typeof (RecentPopularTags)};

            // Arrange.
            // Note: we're faking that no user has been authenticated.
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Index(null, null) as ViewResult;

            // Assert.
            Assert.NotNull(result);

            var model = result.Model as IndexViewModel;
            Assert.NotNull(model);

            UserTagListViewModel userFavoriteTagListViewModel = model.UserFavoriteTagListViewModel;
            Assert.NotNull(userFavoriteTagListViewModel);

            Assert.Equal("Favorite Tags", userFavoriteTagListViewModel.Header);
            Assert.Equal("interesting-tags", userFavoriteTagListViewModel.DivId1);
            Assert.Equal("interestingtags", userFavoriteTagListViewModel.DivId2);
            Assert.Null(userFavoriteTagListViewModel.Tags);
        }

        [Fact]
        public void GivenSomeQuestionsAndNoDisplayNameAndNoTags_Index_ReturnsAJsonViewOfMostRecentQuestions()
        {
            IndexesToExecute = new List<Type> {typeof (Questions_Search)};

            // Arrange.
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            // Note: this should return a list of the 20 most recent.
            JsonResult result = homeController.IndexJson(null, null);

            // Assert.
            Assert.NotNull(result);
            var questions = result.Data as IList<QuestionWithDisplayName>;
            Assert.NotNull(questions);
            Assert.Equal(20, questions.Count);

            // Now lets Make sure each one is ok.
            DateTime? previousDate = null;
            foreach (QuestionWithDisplayName question in questions)
            {
                if (previousDate.HasValue)
                {
                    Assert.True(previousDate.Value > question.CreatedOn);
                }

                previousDate = question.CreatedOn;
                Assert.NotNull(question.DisplayName);
                Assert.NotNull(question.Id);
                Assert.NotNull(question.CreatedByUserId);
                Assert.NotNull(question.Subject);
                Assert.NotNull(question.Content);
            }
        }

        [Fact]
        public void GivenSomeQuestionsAndATag_Index_ReturnsAViewResult()
        {
            IndexesToExecute = new List<Type> {typeof (Questions_Search), typeof (RecentPopularTags)};

            // Arrange.
            const string tag = "ravendb";
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Index(null, tag) as ViewResult;

            // Assert.
            Assert.NotNull(result);

            var model = result.Model as IndexViewModel;
            Assert.NotNull(model);
            Assert.NotNull(model.QuestionListViewModel);
            Assert.NotNull(model.QuestionListViewModel.Questions);

            // Make sure each question is tagged with the defined tag-value.
            Assert.True(model.QuestionListViewModel.Questions.All(x => x.Tags.Contains(tag)));
        }

        [Fact]
        public void GivenSomeQuestionsAndAnExistingTag_Tags_ReturnsAListOfTaggedQuestions()
        {
            // Arrange.
            const string tag = "ravendb";
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Tag(tag) as JsonResult;

            // Assert.
            Assert.NotNull(result);

            dynamic model = result.Data;
            Assert.NotNull(model);

            // At least 5 questions are hardcoded to include the RavenDb tag.
            Assert.NotNull(model.Questions);
            Assert.True(model.Questions.Count >= 5);
            Assert.True(model.TotalResults >= 5);
        }

        [Fact]
        public void GivenSomeQuestionsAndAnExistingTag_Search_ReturnsAListOfTags()
        {
            IndexesToExecute = new List<Type> {typeof (RecentPopularTags)};

            // Arrange.
            const string tag = "ravendb";
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Search(tag) as JsonResult;

            // Assert.
            Assert.NotNull(result);
            dynamic model = result.Data;
            Assert.NotNull(model);
            Assert.Equal(1, model.Count);
            Assert.Equal("ravendb", model[0]);
        }

        [Fact]
        public void GivenSomeQuestionsAndAnExistingPartialTag_Search_ReturnsAListOfTaggedQuestions()
        {
            IndexesToExecute = new List<Type> {typeof (RecentPopularTags)};

            // Arrange.
            const string tag = "ravne"; // Hardcoded Typo.
            var homeController = new HomeController(DocumentSession);
            ControllerUtilities.SetUpControllerContext(homeController);

            // Act.
            var result = homeController.Search(tag) as JsonResult;

            // Assert.
            Assert.NotNull(result);

            dynamic model = result.Data;
            Assert.NotNull(model);
            Assert.Equal(1, model.Count);
            Assert.Equal("ravendb", model[0]);
        }
    }

    // ReSharper restore InconsistentNaming
}