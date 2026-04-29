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
            postToBoard: { url: 'api/Loads/PostToBoard', method: 'POST', params: {}, isArray: false },
            setWorkflow: { url: 'api/Loads/SetWorkflowStatus', method: 'POST', params: {}, isArray: false },
            templatesList: { url: 'api/Loads/Templates/List', method: 'GET', params: {}, isArray: false },
            templateSave: { url: 'api/Loads/Templates/Save', method: 'POST', params: {}, isArray: false },
            loadClaimsList: { url: 'api/LoadClaims/ListForLoad/:loadId', method: 'GET', isArray: true },
            submitClaim: { url: 'api/LoadClaims/Submit', method: 'POST', params: {}, isArray: false },
            acceptClaim: { url: 'api/LoadClaims/Accept', method: 'POST', params: {}, isArray: false },
            rejectClaim: { url: 'api/LoadClaims/Reject', method: 'POST', params: {}, isArray: false },
        });
    };
})();
