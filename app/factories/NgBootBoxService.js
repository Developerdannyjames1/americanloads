(function () {

	'use strict';

	angular.module('ASTDAT')
		.service('NgBootBoxService', ['$ngBootbox', NgBootBoxService]);

	function NgBootBoxService($ngBootbox) {
		return {
			prompt: function () {
			},

			alert: function (message, title, callbackOk) {
				var options = {
					message: message,
					title: title,
					buttons: {
						yes: {
							label: 'Ok',
							className: 'btn-success',
							callback: function () {
								if (callbackOk) {
									callbackOk();
								}
							}
						},
					}
				};
				$ngBootbox.customDialog(options);
			},

			confirm: function (message, title, callbackYes, callbackNo) {
				var options = {
					message: message,
					title: title,
					buttons: {
						yes: {
							label: 'Yes',
							className: 'btn-success',
							callback: function () {
								if (callbackYes) {
									callbackYes();
								}
							}
						},
						no: {
							label: 'No',
							className: 'btn-warning',
							callback: function () {
								if (callbackNo) {
									callbackNo();
								}
							}
						}
					}
				};
				$ngBootbox.customDialog(options);
			},
		};
	};
})();
