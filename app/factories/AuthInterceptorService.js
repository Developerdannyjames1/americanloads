(function () {

    'use strict';

    angular.module('ASTDAT')
        .service('AuthInterceptorService', ['$q', '$rootScope', AuthInterceptorService]);

    function AuthInterceptorService($q, $rootScope) {

        var authInterceptorServiceFactory = {};

        var _request = function (config) {
            $rootScope.globalLoading = true;

            return config;
        };

        var _responseError = function (rejection) {
            $rootScope.globalLoading = false;

            return $q.reject(rejection);
        };

        var _response = function (response) {
            $rootScope.globalLoading = false;

            return response || $q.when(response);
        };

        authInterceptorServiceFactory.request = _request;
        authInterceptorServiceFactory.responseError = _responseError;
        authInterceptorServiceFactory.response = _response;

        return  {
            request: _request,
            response: _response,
            responseError: _responseError,
        };
    };
})();
