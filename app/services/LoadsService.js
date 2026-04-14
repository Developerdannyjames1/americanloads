(function () {

    'use strict';

    angular.module('ASTDAT')
        .service('LoadsService', ['$resource', LoadsService]);

    function LoadsService($resource) {
        return $resource('api/Loads', {}, {
            list: { url: 'api/Loads/List', method: 'POST', params: {}, isArray: false },
            delete: { url: 'api/Loads/Delete', method: 'POST', params: {}, isArray: false },
            addLoad: { url: 'api/Loads/AddLoad', method: 'POST', params: {}, isArray: false },
            updateDAT: { url: 'api/Loads/UpdateDAT', method: 'POST', params: {}, isArray: false },
            uploadLoad: { url: 'api/Loads/UploadLoad', method: 'POST', params: {}, isArray: false },
            importLog: { url: 'api/Loads/ImportLog', method: 'POST', params: {}, isArray: false },
            getComments: { url: 'api/Loads/GetComments', method: 'GET', params: {}, isArray: false },
            getCities: { url: 'api/Loads/GetCities', method: 'GET', params: {}, isArray: false },
			recover: { url: 'api/Loads/Recover', method: 'POST', params: {}, isArray: false },
        });
    };
})();
