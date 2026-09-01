(function () {
	'use strict';

	function dismissToast(toast) {
		if (!toast) {
			return;
		}

		toast.classList.add('app-toast--leaving');
		window.setTimeout(function () {
			toast.remove();
		}, 220);
	}

	document.querySelectorAll('[data-app-toast]').forEach(function (toast) {
		var dismissButton = toast.querySelector('[data-toast-dismiss]');

		if (dismissButton) {
			dismissButton.addEventListener('click', function () {
				dismissToast(toast);
			});
		}

		window.setTimeout(function () {
			dismissToast(toast);
		}, 4500);
	});
})();
