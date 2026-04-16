(function () {
    ROOT = $('base').attr('href');

    'use strict';

    angular.module('ASTDAT', [
        'ngResource',
        'ngMessages',
        'ngSanitize',
        'ngBootbox',
        'ngAnimate',
        'angular.filter',

        'angularMoment',
        'ui.bootstrap',
        'datetimepicker',
        'toastr',
        'angucomplete-alt',
        'ui.select',

        'ase.com.ua',
        'ASTDAT',
    ]);

    angular.module('ASTDAT')
        .config(['$httpProvider', 'uiSelectConfig',
            function ($httpProvider, uiSelectConfig) {
                $httpProvider.interceptors.push('AuthInterceptorService');
                uiSelectConfig.removeSelected = false;
            }])
        .run([
            function ( ) {
			}]);

	angular.module('ASTDAT').directive('myUiSelect', function () {
		return {
			require: 'uiSelect',
			link: function (scope, element, attrs, $select) {
				scope.$on('closeAll', function () {
					$select.close();
				});

				var searchInput = element.querySelectorAll('input.ui-select-search');

				searchInput.on('keydown', { select: $select }, function (event) {
					if (event.keyCode === 9
						&& event.data.select.ngModel.$modelValue.length === 1
						&& event.data.select.ngModel.$modelValue[0] === '') {
						event.data.select.ngModel.$modelValue.length = event.data.select.ngModel.$modelValue.length - 1;
						event.data.select.close();
						event.data.select.setFocus();
					}
				});
			}
		};
	});

	angular.module('ASTDAT').directive('myDisableTab', function () {
		return {
			require: 'uiSelect',
			link: function (scope, element, attrs, $select) {
				var searchInput = element.querySelectorAll('input.ui-select-search');

				searchInput.on('keydown', { select: $select }, function (event) {
					if (event.keyCode === 9) {
						l(event.data.select);
						event.data.select.ngModel.$modelValue.length = event.data.select.ngModel.$modelValue.length - 1;
						event.data.select.close();
						event.data.select.setFocus();
					}
				});
			}
		};
	});
})();
