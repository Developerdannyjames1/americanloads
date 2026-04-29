(function () {
    'use strict';

    var base = document.querySelector('base');
    var root = (base && base.getAttribute('href')) || '/';
    if (root.charAt(root.length - 1) !== '/') {
        root += '/';
    }

    var afterEventId = 0;
    var lastRow = null;

    function doSearch() {
        var lane = document.getElementById('cp-lane').value;
        var equip = document.getElementById('cp-equip').value;
        var df = document.getElementById('cp-df').value;
        var dt = document.getElementById('cp-dt').value;
        var err = document.getElementById('cp-err');
        err.style.display = 'none';

        var payload = {
            Page: 1,
            PerPage: 100,
            Sort: '',
            SortDate: 2,
            ShowDeleted: false,
            LaneSearch: lane,
            EquipmentTypeSearch: equip,
            DateFrom: df ? df : null,
            DateTo: dt ? dt : null
        };
        if (!window.jQuery) {
            err.textContent = 'jQuery is required for this page.';
            err.style.display = 'block';
            return;
        }
        window.jQuery
            .ajax({
                url: root + 'api/Loads/List',
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(payload)
            })
            .done(function (res) {
                if (res && (res.exception1 || res.Exception1)) {
                    err.textContent = String(res.exception1 || res.Exception1);
                    err.style.display = 'block';
                    return;
                }
                renderList(res);
            })
            .fail(function (x) {
                var msg = (x && x.responseJSON) ? (x.responseJSON.message || x.statusText) : (x && x.statusText) || 'Request failed';
                if (x && x.status === 403) {
                    msg = 'Not authorized to list loads. Your company may not be approved yet.';
                }
                err.textContent = String(msg);
                err.style.display = 'block';
            });
    }
    window.carrierPortalSearch = doSearch;

    function esc(s) {
        if (s == null) { return ''; }
        var t = String(s);
        return t
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    }

    function renderList(res) {
        var list = (res && res.List) != null ? res.List : (res && res.list) != null ? res.list : [];
        var tb = document.getElementById('cp-tbody');
        tb.innerHTML = '';
        lastRow = null;
        document.getElementById('cp-detail').textContent = 'Select a load…';
        document.getElementById('cp-actions').style.display = 'none';

        for (var i = 0; i < list.length; i++) {
            (function (item) {
                var o = (item.Origin) || (item.origin);
                var d = (item.Destination) || (item.destination);
                var oc = o && o.City != null ? o.City : (o && o.city) || '';
                var dc = d && d.City != null ? d.City : (d && d.city) || '';
                var eq = (item.LoadType) && (item.LoadType.Name || item.loadType) ? (item.LoadType.Name) : (item.EquipmentType || item.equipmentType) || '—';
                var lane = esc(oc) + (oc || dc ? ' \u2192 ' : '') + esc(dc);
                if (!lane) { lane = '—'; }
                var st = item.WorkflowStatus != null ? item.WorkflowStatus : (item.workflowStatus) || '—';
                var pick = item.PickUpDate != null ? item.PickUpDate : item.pickUpDate;
                if (typeof pick === 'string' && pick.length > 10) { pick = pick.substring(0, 10); }
                var tr = document.createElement('tr');
                tr.style.cursor = 'pointer';
                tr.innerHTML =
                    '<td>' + esc(item.Id) + '</td><td>' + lane + '</td><td>' + esc(eq) + '</td><td>' + esc(st) + '</td><td>' + esc(pick || '—') + '</td>';
                tr.onclick = function () {
                    if (lastRow) { lastRow.classList.remove('info'); }
                    tr.classList.add('info');
                    lastRow = tr;
                    showDetail(item);
                };
                tb.appendChild(tr);
            })(list[i]);
        }
    }

    function showDetail(item) {
        var o = item.Origin || item.origin;
        var d = item.Destination || item.destination;
        var lines = [];
        lines.push('Load #' + (item.Id || item.id));
        if (o) {
            lines.push('Origin: ' + (o.City || o.city || '—') + ', ' + ((o.State && o.State.Code) || (o.state && o.state.code) || '—') + ' ' + (o.PostalCode || o.postalCode || ''));
        }
        if (d) {
            lines.push('Dest:   ' + (d.City || d.city || '—') + ', ' + ((d.State && d.State.Code) || (d.state && d.state.code) || '—') + ' ' + (d.PostalCode || d.postalCode || ''));
        }
        lines.push('Status: ' + (item.WorkflowStatus || item.workflowStatus || '—'));
        if (item.Description) { lines.push('Notes: ' + String(item.Description)); }
        if (item.Commodity) { lines.push('Commodity: ' + String(item.Commodity)); }
        if (item.PickUpDate) { lines.push('Pickup: ' + String(item.PickUpDate)); }
        if (item.DeliveryDate) { lines.push('Delivery: ' + String(item.DeliveryDate)); }
        document.getElementById('cp-detail').textContent = lines.join('\n');

        var st = (item.WorkflowStatus || item.workflowStatus || '').toString().toLowerCase();
        var canAct = (st === 'posted' || st === 'claimed');
        document.getElementById('cp-actions').style.display = canAct ? 'block' : 'none';
        if (canAct) {
            document.getElementById('cp-claim').onclick = function () { doClaimOrBid('claim', item); };
            document.getElementById('cp-bid').onclick = function () { doClaimOrBid('bid', item); };
        }
    }

    function doClaimOrBid(type, item) {
        var id = item.Id || item.id;
        if (!id) { return; }
        var err = document.getElementById('cp-err');
        err.style.display = 'none';
        var body = { LoadId: id, ClaimType: type, Message: '' };
        if (type === 'bid') {
            var a = window.prompt('Bid amount (number):', '');
            if (a == null) { return; }
            if (!/^\d+(\.\d{1,2})?$/.test((a + '').trim())) {
                err.textContent = 'Enter a valid number for the bid amount.';
                err.style.display = 'block';
                return;
            }
            body.BidAmount = parseFloat(a, 10);
        }
        window.jQuery
            .ajax({
                url: root + 'api/LoadClaims/Submit',
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(body)
            })
            .done(function (r) {
                if (r && r.Ok) {
                    if (window.toastr) { window.toastr.success(type === 'bid' ? 'Bid sent.' : 'Load claimed (pending).'); }
                    doSearch();
                } else {
                    err.textContent = (r && r.message) || 'Failed.';
                    err.style.display = 'block';
                }
            })
            .fail(function (x) {
                var t = (x && x.responseJSON) ? (x.responseJSON.message || (typeof x.responseText === 'string' ? x.responseText : x.status)) : (x && x.statusText) || 'Failed';
                if (t && t.indexOf('{') === 0) { try { t = JSON.parse(t).message || t; } catch (e) { } }
                err.textContent = 'Claim/bid: ' + String(t);
                err.style.display = 'block';
            });
    }

    function clearFilters() {
        var lane = document.getElementById('cp-lane');
        var equip = document.getElementById('cp-equip');
        var from = document.getElementById('cp-df');
        var to = document.getElementById('cp-dt');
        if (lane) { lane.value = ''; }
        if (equip) { equip.value = ''; }
        if (from) { from.value = ''; }
        if (to) { to.value = ''; }
        doSearch();
    }
    window.carrierPortalClear = clearFilters;

    function pollEvents() {
        if (!window.jQuery) { return; }
        window.jQuery
            .getJSON(root + 'api/LoadEvents/Since?afterId=' + afterEventId)
            .done(function (res) {
                var events = (res && res.events) != null ? res.events : [];
                for (var i = 0; i < events.length; i++) {
                    var e = events[i];
                    if (e.Id > afterEventId) { afterEventId = e.Id; }
                }
                var el = document.getElementById('cp-evlast');
                if (el) {
                    el.textContent = events.length ? (events[events.length - 1].Type + ' @ ' + events[events.length - 1].Utc) : (afterEventId > 0 ? 'idle' : '—');
                }
            });
    }

    if (window.jQuery) {
        window.jQuery('#cp-search').on('click', doSearch);
        window.jQuery('#cp-clear').on('click', clearFilters);
        window.jQuery(document).on('click', '#cp-clear', clearFilters);
        window.jQuery(document).on('click', '#cp-search', doSearch);
        doSearch();
        setInterval(pollEvents, 8000);
    } else {
        var err = document.getElementById('cp-err');
        err.textContent = 'jQuery is required. Open the app from a page that includes jQuery.';
        err.style.display = 'block';
    }
})();
