(function () {

    'use strict';

    angular
        .module('ASTDAT')
        .controller('LoadsController',
			['$scope', '$ngBootbox', '$interval', '$timeout', '$http', '$q', 'LoadsService', 'NgBootBoxService', LoadsController]);

	function LoadsController($scope, $ngBootbox, $interval, $timeout, $http, $q, LoadsService, NgBootBoxService) {
        /*eslint max-statements: ["error", 50]*/
        $scope.data = {
            SortDate: 2,
            Sort: '',
        };

		$(document).on('focus', '.ui-select-search', function () {
			$scope.$parent.$root.$broadcast('closeAll', true);
		});

		$(document).on('keydown', '.ui-select-search', function (e) {
			if (e.keyCode === 9) {
				$scope.$parent.$root.$broadcast('closeAll', true);
			}
		});

        $scope.IsDebugMode = true;

        $scope.filter = {
            DateFromD: moment(new Date()).format('L'),
            DateToD: moment(new Date()).format('L'),
        };

        $interval(function () {
            if (!$scope.data.ShowDeleted) {
                $scope.refresh();
            }
        }, 1 * 60 * 1000);

		$scope.refresh = function (page) {
            var q = {
                SortDate: $scope.data.SortDate,
                Sort: $scope.data.Sort,
                DateFrom: $scope.filter.DateFrom,
                DateTo: $scope.filter.DateTo,
                //OriginZip: $scope.filter.OriginZip,
                //DestinZip: $scope.filter.DestinZip,
                Companies: $scope.filter.Companies,
                OriginCities: $scope.filter.OriginCities,
                OriginStates: $scope.filter.OriginStates,
                DestinationCities: $scope.filter.DestinationCities,
                DestinStates: $scope.filter.DestinStates,
				Id: $scope.filter.Id,
				RefId: $scope.filter.RefId,
                Page: page || $scope.data.Page,
                ShowDeleted: $scope.data.ShowDeleted,
                //FullLoad: !$scope.data.AllCities,
            };
            LoadsService.list(q, function (data) {
                $scope.selectedCount = 0;
                $scope.data.ListFiltered = $scope.data.ListFiltered || [];
                var selected = $scope.data.ListFiltered.filter(function (e) {
                    return e.selected === true;
                });
				$scope.data = data;
				var emptyCompany = $scope.data.AllCompanies.find(function (e) {
					return e === '';
				});
				if (emptyCompany) {
					$scope.data.AllCompanies = [''].concat($scope.data.AllCompanies);
				}
                for (var i = 0; i < $scope.data.List.length; i++) {
                    var item = $scope.data.List[i];

                    if (selected.find(function (e) {
                        return e.Id === item.Id;
                    })) {
                        item.selected = true;
                        $scope.selectedCount++;
                    }
                }
                $scope.data.ListFiltered = data.List;
                //$scope.doFilter(data);
            });
        };

        $scope.selectedCount = 0;
        $scope.lastSelected = 0;

        $scope.itemClick = function (item, $event) {
            item.selected = !item.selected;
            if ($event.shiftKey) {
                var idx1 = $scope.data.ListFiltered.findIndex(function (e) {
                    return e.Id === $scope.lastSelected.Id;
                });
                var idx2 = $scope.data.ListFiltered.findIndex(function (e) {
                    return e.Id === item.Id;
                });

                for (var i = Math.min(idx1, idx2); i < Math.max(idx1, idx2); i++) {
                    $scope.data.ListFiltered[i].selected = item.selected;
                    $scope.selectedCount += item.selected ? 1 : -1;
                }
            } else {
                $scope.selectedCount += item.selected ? 1 : -1;
            }
            $scope.lastSelected = item;
            window.getSelection().removeAllRanges();
        };

        $scope.sortDate = function () {
            $scope.data.Sort = '';
            $scope.data.SortDate = $scope.data.SortDate === 1 ? 2 : 1;
            $scope.refresh();
        };

        $scope.sort = function (col) {
            $scope.data.Sort = col;
            $scope.refresh();
        };

        $scope.getSort = function (col) {
            if ($scope.data.Sort.indexOf(col) === -1) {
                return col + '_asc';
            }
            return $scope.data.Sort.indexOf('_asc') === -1 ? col + '_asc' : col + '_desc';
        };

        $scope.originDestinations = [];

        $scope.showFilterDate = function () {
            $('#modalDates').modal('show');
        };

        $scope.filterDate = function () {
            $('#modalDates').modal('hide');
            $scope.filter.DateFrom = $scope.filter.DateFromD;
            $scope.filter.DateTo = $scope.filter.DateToD;
            $scope.refresh();
        };

        $scope.showFilterZip = function (mode) {
            $scope.filterZipMode = mode;
            $('#modalZipRange').modal('show');
        };

        $scope.filterZip = function () {
            $('#modalZipRange').modal('hide');
            if ($scope.filterZipMode === 1) {
                $scope.filter.DestinZipFrom = $scope.filter.RangeFrom;
                $scope.filter.DestinZipTo = $scope.filter.RangeTo;
            } else {
                $scope.filter.OriginZipFrom = $scope.filter.RangeFrom;
                $scope.filter.OriginZipTo = $scope.filter.RangeTo;
            }
            $scope.refresh();
        };

        $scope.delete = function () {
            var dat = $scope.data.ListFiltered.filter(function (e) {
                if ($scope.data.ShowDeleted) {
                    return e.selected && e.AssetId && e.DateDatDeleted;
                }
                return e.selected && e.AssetId && !e.DateDatDeleted;
            });
            var ts = $scope.data.ListFiltered.filter(function (e) {
                if ($scope.data.ShowDeleted) {
                    return e.selected && e.TrackStopId && e.DateTSDeleted;
                }
                return e.selected && e.TrackStopId && !e.DateTSDeleted;
            });
            /*var selected = $scope.data.ListFiltered.filter(function (e) {
                if ($scope.data.ShowDeleted) {
                    return e.selected;
                }
                return e.selected;
            });*/
            var options = {};
            if (dat.length === 0 && ts.length === 0 && $scope.selectedCount === 0) {
                options = {
                    message: 'Please select one or more load to be deleted.',
                    title: 'Information',
                    className: 'test-class',
                    buttons: {
                        success: {
                            label: 'Close',
                            className: 'btn-success',
                            callback: function () {
                            }
                        }
                    }
                };
			} else {
                var message = $scope.data.ShowDeleted ?
					'Will permanently delete ' + $scope.selectedCount + ' ' + ($scope.selectedCount === 1 ? 'asset' : 'assets') + '? ' :
					'Delete ' + $scope.selectedCount + ' ' + ($scope.selectedCount === 1 ? 'asset' : 'assets')
					+ ' from the loadboards. They will be put in the "deleted" list. Click on "Show deleted" to see them.';

				if (!$scope.data.ShowDeleted) {
					var unitedRentals = $scope.data.ListFiltered.filter(function (e) {
						return e.selected
							&& (e.ClientName || '').toLowerCase() === 'UNITED RENTALS'.toLowerCase()
							&& !e.DateDatDeleted
							&& !e.DateTSDeleted;
					});
					if (unitedRentals.length > 0) {
						var message2 = 'Will permanently delete ' + unitedRentals.length + ' selected United Rentals loads';
						if (unitedRentals.length === $scope.selectedCount) {
							message = message2;
						} else {
							message = message2 + '<br><br>' + 'Delete ' + ($scope.selectedCount - unitedRentals.length) + ' ' + ($scope.selectedCount === 1 ? 'asset' : 'assets')
								+ ' from the loadboards. They will be put in the "deleted" list. Click on "Show deleted" to see them.';
						}
					}
				}

                options = {
                    message: message,
                    title: 'Confrim',
                    className: 'test-class',
                    buttons: {
                        warning: {
                            label: 'Yes',
                            className: 'btn-warning',
                            callback: function () {
                                var ids = $scope.data.ListFiltered.filter(function (e) {
                                    return e.selected;
                                });
                                ids = ids.map(function (e) {
                                    return e.Id;
                                });
                                LoadsService.delete({ ids: ids, Eliminate: $scope.data.ShowDeleted }, function () {
                                    $scope.refresh();
                                });
                            }
                        },
                        success: {
                            label: 'Close',
                            className: 'btn-success',
                            callback: function () {
                            }
                        }
                    }
                };
                if (dat.length > 0 && ts.length > 0 && !$scope.data.ShowDeleted) {
                    options.buttons = {
                        warning: {
                            label: 'Delete DAT and TS',
                            className: 'btn-warning',
                            callback: function () {
                                var ids = $scope.data.ListFiltered.filter(function (e) {
                                    //return e.selected && (e.AssetId || e.TrackStopId);
                                    return e.selected;
                                });
                                ids = ids.map(function (e) {
                                    return e.Id;
                                });
                                LoadsService.delete({ ids: ids }, function () {
                                    $scope.refresh();
                                });
                            }
                        },
                        warning2: {
                            label: 'Delete Only DAT',
                            className: 'btn-warning',
                            callback: function () {
                                var ids = $scope.data.ListFiltered.filter(function (e) {
                                    return e.selected && e.AssetId && !e.DateDatDeleted;
                                });
                                ids = ids.map(function (e) {
                                    return e.Id;
                                });
                                LoadsService.delete({ ids: ids, dat: true }, function () {
                                    $scope.refresh();
                                });
                            }
                        },
                        warning3: {
                            label: 'Delete Only TS',
                            className: 'btn-warning',
                            callback: function () {
                                var ids = $scope.data.ListFiltered.filter(function (e) {
                                    return e.selected && e.TrackStopId && !e.DateTSDeleted;
                                });
                                ids = ids.map(function (e) {
                                    return e.Id;
                                });
                                LoadsService.delete({ ids: ids, ts: true }, function () {
                                    $scope.refresh();
                                });
                            }
                        },
                        success: {
                            label: 'Close',
                            className: 'btn-success',
                            callback: function () {
                            }
                        },
                    };
                }
            }

            $ngBootbox.customDialog(options);
        };

        $scope.changeShowDeleted = function () {
            $scope.data.ShowDeleted = !$scope.data.ShowDeleted;
            /*$scope.data.ListFiltered.map(function (e) {
                e.selected = false;
            });
            $scope.selectedCount = 0;*/
            //$scope.refresh();
			$scope.clearFilters();
        };

        $scope.clearSelections = function () {
            var select = $scope.selectedCount === 0 ? true : false;
            for (var i = 0; i < $scope.data.List.length; i++) {
                $scope.data.List[i].selected = select;
            }
            $scope.selectedCount = select ? $scope.data.List.length : 0;
        };

        $scope.clearFilters = function () {
            $scope.filter.DestinationCities = '';
            $scope.filter.DestinStates = '';
            $scope.filter.OriginCities = '';
            $scope.filter.OriginStates = '';
            $scope.filter.Companies = '';
            $scope.filter.DateFrom = undefined;
            $scope.filter.DateTo = undefined;
			$scope.filter.Id = '';
			$scope.filter.RefId = '';
            $scope.refresh();
        };

        $scope.dateFilter = function (mode, days) {
            if (mode === 1) {
                $scope.filter.DateFromD = moment($scope.filter.DateFromD).add(days, 'days').format('L');
            } else {
                $scope.filter.DateToD = moment($scope.filter.DateToD).add(days, 'days').format('L');
            }
        };

        $scope.editLoad = function (load, form) {
            $scope.$broadcast('angucomplete-alt:clearInput');
            form.$submitted = false;
            if (load) {
                load.selected = !load.selected;
                $scope.selectedCount += load.selected ? 1 : -1;
                load.Origin.StateCode = load.Origin.State.Code;
                load.Destination.StateCode = load.Destination.State.Code;
            }
            $scope.currentLoad = load || {
                id: 0,
                DateLoaded: new Date(),
                ClientName: $scope.data.AllCompanies.length > 0 ? $scope.data.AllCompanies[0] : '',
                CarrierAmount: 0,
            };

            $('#modalLoad').modal('show');
            $timeout(function () {
				$('#modalLoad .ClientLoadNum').focus();
                //$scope.$broadcast('UiSelectLoadType');
            }, 500);
            $timeout(function () {
                $scope.$broadcast('angucomplete-alt:changeInput', 'OriginAC', $scope.currentLoad.Origin);
                $scope.$broadcast('angucomplete-alt:changeInput', 'DestinationAC', $scope.currentLoad.Destination);
            }, 200);
        };

        $scope.updateDAT = function (form) {
            $ngBootbox.confirm('Origin, Destination, Pickup and Delivery dates will NOT be updated')
                .then(function () {
                    $scope.saveLoad(form, true);
                }, function () {
                });
        };

		$scope.saveLoad = function (form, updateDAT) {
			if ($scope.originDestinCityInvalid) {
				return;
			}
            if ($scope.currentLoad.PickUpDate) {
                $scope.currentLoad.PickUpDate = moment($scope.currentLoad.PickUpDate).startOf('day').toDate();
                if ($scope.currentLoad.PickUpDateTime) {
                    $scope.currentLoad.PickUpDate = moment($scope.currentLoad.PickUpDate)
                        .add(moment($scope.currentLoad.PickUpDateTime).hour(), 'h')
                        .add(moment($scope.currentLoad.PickUpDateTime).minute(), 'm')
                        .toDate();
                }
            }
            if ($scope.currentLoad.DeliveryDate) {
                $scope.currentLoad.DeliveryDate = moment($scope.currentLoad.DeliveryDate).startOf('day').toDate();
                if ($scope.currentLoad.DeliveryDateTime) {
                    $scope.currentLoad.DeliveryDate = moment($scope.currentLoad.DeliveryDate)
                        .add(moment($scope.currentLoad.DeliveryDateTime).hour(), 'h')
                        .add(moment($scope.currentLoad.DeliveryDateTime).minute(), 'm')
                        .toDate();
                }
            }
            form.$submitted = true;
            if (!form.$valid) {
                if (form.LoadType.$invalid) {
                    $('[name="LoadType"]').find('input').focus();
                } else if (form.Origin.$invalid || form.OriginState.$invalid) {
                    $('[name="Origin"]').find('input').focus();
                } else if (form.Destination.$invalid || form.DestinationState.$invalid) {
                    $('[name="Destination"]').find('input').focus();
                } else if (form.PickUpDate.$invalid) {
                    $('[name="PickUpDate"]').find('input').focus();
                }
                return;
            }
            $scope.currentLoad.AssetLength = parseInt($scope.currentLoad.AssetLength);
            $scope.currentLoad.Uploaded = null;
            var fn = LoadsService.addLoad;
            if (updateDAT) {
                fn = LoadsService.updateDAT;
			}
			if (!$scope.currentLoad.Origin.Id || !$scope.currentLoad.Destination.Id) {
				var s = [];
				if (!$scope.currentLoad.Origin.Id) {
					s.push('' + $scope.currentLoad.Origin.City + ', ' + $scope.currentLoad.Origin.State.Code);
				}
				if (!$scope.currentLoad.Destination.Id) {
					s.push('' + $scope.currentLoad.Destination.City + ', ' + $scope.currentLoad.Destination.State.Code);
				}

				NgBootBoxService.confirm(
					'Would you like to add ' + s.join(' and ') + ' to the city list',
					'AST Loads App',
					function () {
						$('#modalLoadingToLoadBoard').modal('show');
						fn($scope.currentLoad, function (data) {
							data.model.Origin.StateCode = data.model.Origin.State.Code;
							data.model.Destination.StateCode = data.model.Destination.State.Code;
							$scope.currentLoad = data.model;
							$scope.currentLoad.Uploaded = true;
							$scope.uploadErrors = data.errors;
							$scope.refresh();
						});
					}
				);
			} else {
				$('#modalLoadingToLoadBoard').modal('show');
				fn($scope.currentLoad, function (data) {
					data.model.Origin.StateCode = data.model.Origin.State.Code;
					data.model.Destination.StateCode = data.model.Destination.State.Code;
					$scope.currentLoad = data.model;
					$scope.currentLoad.Uploaded = true;
					$scope.uploadErrors = data.errors;
					$scope.refresh();
				});
			}
        };

        $scope.uploadModalHide = function () {
            $('#modalLoad').modal('hide');
            $('#modalLoadingToLoadBoard').modal('hide');
        };

        var prev = undefined;
        $scope.refreshCompany = function ($select) {
            var search = $select.search,
                list = angular.copy($select.items);
            //remove last user input
            list = list.filter(function (item) {
                return item !== prev;
            });
            prev = search;

            if (!search) {
                $select.items = list;
            } else {
                //manually add user input and set selection
                var userInputItem = search;
                $select.items = [userInputItem].concat(list);
                $select.selected = userInputItem;
            }
        };

        $scope.setOriginDestinations = function (prop) {
            if (!$scope.currentLoad) {
                return;
			}
			if ($scope.currentLoad.OriginAC && prop === 'Origin') {
				$scope.currentLoad.Origin = $scope.currentLoad.OriginAC.originalObject;
			}
            if ($scope.currentLoad.DestinationAC && prop === 'Destination') {
                $scope.currentLoad.Destination = $scope.currentLoad.DestinationAC.originalObject;
            }
        };

        $scope.refreshOrigDestin = function ($select) {
            var search = $select.search.toUpperCase(),
                list = angular.copy($select.items);
            //remove last user input
            list = list.filter(function (item) {
                return item.FLAG !== -1;
            });

            if (!search) {
                $select.items = list;
            } else {
                //manually add user input and set selection
                var items = search.split(' ');
                var code = '';
                if (items.length > 1) {
                    code = items[items.length - 1].substring(0, 2).toUpperCase();
                }

                var userInputItem = {
                    //City: search.split(' ')[0],
                    City: search,
                    State: {
                        Code: code
                    },
                    FLAG: -1,
                };
                //userInputItem = search;
                $select.items = [userInputItem].concat(list);
                $select.selected = userInputItem;
            }
        };

        $scope.startImport = function (upload) {
            $scope.importResult = {};
            if (!upload) {
                $scope.importFile = undefined;
                $('#modalImport').modal('show');
                $('#importFile').val('');
                return;
            }
            var file = $scope.importFile;
            var fd = new FormData();
            fd.append('file', file);
            fd.append('lastModifiedDate', moment(file.lastModifiedDate).format('YYYY/MM/DD HH:mm:ss'));
            fd.append('name', file.name);
            fd.append('size', file.size);

            $http.post('/api/Loads/Import', fd, {
                transformRequest: angular.identity,
                headers: { 'Content-Type': undefined }
            }).then(function successCallback(response) {
                $scope.importResult = response.data;
                $scope.refresh();
            }, function errorCallback() {
            });
        };

        $scope.viewImportLog = function () {
            $scope.importLog = {};
            LoadsService.importLog({}, function (data) {
                $scope.importLog.list = data.List;
            });
        };

        $scope.refreshDAT = function () {
            var q1 = $http.get('/Integration/DATLoadState');
            var q2 = $http.get('/Integration/TSLoadState');

            $q.all([q1, q2])
                .then(function successCallback() {
                    $scope.refresh();
                }, function errorCallback() {
                });
        };

        $scope.getTableHeigth = function () {
            return window.innerHeight - 335;
        };

        $scope.showViewRecord = function () {
            LoadsService.getComments({ id: $scope.currentLoad.Id }, function (data) {
                $scope.currentLoad.Comments = data.Comments;
                $('#modalViewRecord').modal();
            });
        };

		$scope.searchCity = function ($select) {
            $scope.Cities = [];
            if ($select && $select.search && $select.search.length > 0) {
                LoadsService.getCities({ str: $select.search }, function (data) {
                    $scope.Cities = data.Cities;
                });
            }
        };

        $scope.startExport = function () {
            var csv = [];

			var str = 'Starting City,Starting State,Starting Country,Starting Zip,Destination City,';
			str += 'Destination State, Destination Country, Destination Zip, Type of Equipment, Rate, ';
			str += 'Pickup Date, PickUp Time, Delivery Date, Delivery Time, TL or LTL, Equipment Options,';
			str += 'Weight, Length, Width, Stops, Distance, Quantity, Special Information, IsDaily';
            csv.push(str);

            for (var i = 0; i < $scope.data.ListFiltered.length; i++) {

                var load = $scope.data.ListFiltered[i];

                var row = [];
                row.push(load.Origin.City); //Starting City
                row.push(load.Origin.State.Code); //Starting State
                row.push(load.Origin.Country); //Starting Country
                row.push(load.Origin.PostalCode); //Starting Zip
                row.push(load.Destination.City); //Destination City
                row.push(load.Destination.State.Code); //Destination State
                row.push(load.Destination.Country); //Destination Country
                row.push(load.Destination.PostalCode); //Destination Zip
                row.push(load.LoadType.Name); //Type of Equipment
                row.push(load.CarrierAmount); //Rate
                row.push(load.PickUpDate ? moment(load.PickUpDate).format('MM/DD/YYYY') : ''); //Pickup Date
                row.push(load.PickUpDateTime); //PickUp Time (may be not works)
                row.push(load.DeliveryDate ? moment(load.DeliveryDate).format('MM/DD/YYYY') : ''); //Delivery Date
                row.push(load.DeliveryDateTime); //Delivery Time (may be not works)
                row.push(load.IsLoadFull ? 'TL' : 'LTL'); //TL or LTL
                row.push(''); //Equipment Options ?
                row.push(load.Weight); //Weight
                row.push(load.AssetLength); //Length
                row.push(''); //Width ?
                row.push(load.Stops); //Stops
                row.push(''); //Distance ?
                row.push(''); //Quantity ?
                var descr = (load.Description || '').split('\r\n').join(' ');
                descr = descr.split('\r').join(' ');
                descr = descr.split('\n').join(' ');
                row.push(descr); //Special Information
                row.push(''); //IsDaily

                csv.push(row.join(','));
            }

            csv = csv.join('\n');
            var csvFile;
            var downloadLink;

            // CSV FILE
            csvFile = new Blob([csv], { type: 'text/csv' });

            // Download link
            downloadLink = document.createElement('a');

            // File name
            downloadLink.download = 'ASTLoads.csv';

            // We have to create a link to the file
            downloadLink.href = window.URL.createObjectURL(csvFile);

            // Make sure that the link is not displayed
            downloadLink.style.display = 'none';

            // Add the link to your DOM
            document.body.appendChild(downloadLink);

            // Lanzamos
            downloadLink.click();
		};

		$scope.originSearch = function (str, list) {
			return $scope.originDestinSearch(str, list, 1);
		};

		$scope.destinSearch = function (str, list) {
			return $scope.originDestinSearch(str, list, 2);
		};

		$scope.originDestinSearch = function (str, list, mode) {
			$scope.originDestinCityInvalid = 0;
			var matches = [];
			list.forEach(function (city) {
				if (city.City.toUpperCase().indexOf(str.toUpperCase()) !== -1) {
					matches.push(city);
				}
			});
			if (matches.length === 0) {
				var s = str.split(' ');
				if (s.length < 2) {
					$scope.originDestinCityInvalid = mode;
					$scope.originDestinCityInvalidText = 'No results found, and cannot add city because last word is NOT a state';
				} else {
					var f = $scope.data.AllStates.find(function (e) {
						return e.toUpperCase() === s[s.length - 1].toUpperCase();
					});
					var city = s.slice(0, s.length - 1).join(' ').toUpperCase();
					if (!f) {
						$scope.originDestinCityInvalid = mode;
						$scope.originDestinCityInvalidText = 'No results found, and cannot add city because last word is NOT a state';
					} else if (mode === 1) {
						$scope.currentLoad.Origin = {
							City: city, State: { Code: s[s.length - 1].toUpperCase() }
						};
					} else if (mode === 2) {
						$scope.currentLoad.Destination = {
							City: city, State: { Code: s[s.length - 1].toUpperCase() }
						};
					}
				}
			}
			return matches;
		};

		$scope.recover = function () {
			var selected = $scope.data.ListFiltered.filter(function (e) {
				return e.selected === true;
			});
			if (selected.length === 0) {
				NgBootBoxService.alert('Please select one or more loads to recover', 'AST Loads App');
				return;
			}
			NgBootBoxService.confirm(
				'Ready to recover ' + selected.length + ' selected loads?',
				'AST Loads App',
				function () {
					var ids = selected.map(function (e) {
						return e.Id;
					});
					LoadsService.recover(ids, function () {
						$scope.data.ShowDeleted = false;
					});
				});
		};

		$scope.orginDestinFocusOut = function (mode) {
			if (mode === 1 && $scope.currentLoad.Origin) {
				$scope.currentLoad.Origin.City = $scope.currentLoad.Origin.City.split(',').join('');
				$scope.$broadcast('angucomplete-alt:changeInput', 'OriginAC', $scope.currentLoad.Origin.City);
			} else if (mode === 2 && $scope.currentLoad.Destination) {
				$scope.currentLoad.Destination.City = $scope.currentLoad.Destination.City.split(',').join('');
				$scope.$broadcast('angucomplete-alt:changeInput', 'DestinationAC', $scope.currentLoad.Destination.City);
			}
		};

		$scope.noWrap = function (str, max) {
			if (!str || str.length <= max) {
				return str;
			}

			return str.substring(0, max - 1) + '..';
		};

		$scope.toUpperCase = function (str, max) {
			if (!str || str.length <= max) {
				return '';
			}

			return str.toUpperCase();
		};
	};
}());
