// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var xmlns = "http://www.w3.org/2000/svg";

pipe = function (x, y, height, width, mouth) {
	var polygon = document.createElementNS(xmlns, "polygon");
	var mwidth = height / 60
	polygon.setAttribute("style", "fill:black;stroke:orange;")
	var points = (x + width / 2).toString() + ',' + (y - mouth).toString()
	points += ' ' + (x - width / 2).toString() + ',' + (y - mouth).toString()
	points += ' ' + (x - width / 2).toString() + ',' + (y - mouth - height).toString()
	points += ' ' + (x + width / 2).toString() + ',' + (y - mouth - height).toString()
	points += ' ' + (x + width / 2).toString() + ',' + (y - mouth).toString()
	points += ' ' + x.toString() + ',' + y.toString()
	points += ' ' + (x - width / 2).toString() + ',' + (y - mouth).toString()
	points += ' ' + (x - width / 2).toString() + ',' + (y - mouth - mwidth).toString()
	points += ' ' + (x + width / 2).toString() + ',' + (y - mouth - mwidth).toString()
	polygon.setAttribute("points", points);
	return polygon
}
plateface = function (nbPipes, x, y, firstH, lastH, width, firstM, lastM, symetric) {
	var deltaH = (lastH - firstH) / (nbPipes - 1)
	var deltaM = (lastM - firstM) / (nbPipes - 1)
	if (symetric) { deltaH *= 2; deltaM *= 2; }
	var h = firstH
	var m = firstM
	var padding = 0
	for (let p = 0; p < nbPipes; p++) {
		if (symetric && p == ~~(nbPipes / 2)) {
			deltaH *= -1
			deltaM *= -1
		}
		document.getElementById('logo').appendChild(pipe(x + p * (width + padding), y, h, width, m))
		h += deltaH
		m += deltaM
	}
	return x + width / 2 + (nbPipes - 1) * (width + padding)
}
logo = function (x0, y0, scale) {
	var s = scale
	var nA = 3, nB = 5, nC = 10, nD = 9
	var dy = 7 * s, p = 4.5 * s, wA = 5 * s, wB = 3 * s, wC = 3 * s, wD = 2 * s
	var x1 = plateface(nA, x0, y0 - dy, s * 77, s * 88, wA, s * 17, s * 17, true) + p
	var x2 = plateface(nB, x1, y0 - dy, s * 68, s * 42, wB, s * 13, s * 17, false) + p
	var x3 = plateface(nC, x2, y0, s * 42, s * 30, wC, s * 11, s * 11, false) + p
	var x4 = plateface(nD, x3, y0, s * 32, s * 41, wD, s * 11, s * 6, true) + p
	var x5 = plateface(nC, x4, y0, s * 30, s * 42, wC, s * 11, s * 11, false) + p
	var x6 = plateface(nB, x5, y0 - dy, s * 42, s * 68, wB, s * 17, s * 13, false) + p
	var x7 = plateface(nA, x6, y0 - dy, s * 77, s * 88, wA, s * 17, s * 17, true) + p
}

logo(10, 120, 1)