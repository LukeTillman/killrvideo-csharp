var fs = require('fs'),
    vm = require('vm'),
    gulp = require('gulp'),
    rjs = require('gulp-requirejs'),
    uglify = require('gulp-uglify'),
    concat = require('gulp-concat'),
    size = require('gulp-size'),
    merge = require('deeply');

// Read the requirejs runtime config from the scripts folder and echo it so we get the require object back
var requireJsRuntimeConfig = vm.runInNewContext(fs.readFileSync('scripts/requirejs-config.js') + '; require;');

// Merge the runtime config with some settings needed by the optimizer at build time
var requireJsOptimizerConfig = merge(requireJsRuntimeConfig, {
    out: 'scripts.js',
    name:
});