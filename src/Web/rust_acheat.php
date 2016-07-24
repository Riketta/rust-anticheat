<?php
require_once("./rust_acheat_cfg.php");

if (isset($_GET["ver"]))
	echo $LauncherVer;

if (isset($_GET["update"]))
{
	$dir_iterator = new RecursiveDirectoryIterator('./files/');
	$iterator = new RecursiveIteratorIterator($dir_iterator, RecursiveIteratorIterator::SELF_FIRST);
	foreach ($iterator as $file) 
		if($file->isFile())
			echo $file.":".filesize($file)."|"; // Путь_к_файлу:размер_в_байтах|
}
?>