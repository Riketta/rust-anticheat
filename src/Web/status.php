<?php
ob_implicit_flush();

$service_port = 28013;
$address = "127.0.0.1";

$socket = @socket_create(AF_INET, SOCK_STREAM, SOL_TCP);

$result = @socket_connect($socket, $address, $service_port);
$out = @socket_read($socket, 2048);
@socket_close($socket);

$Users = explode("|", $out);

$UserList = "";
foreach ($Users as $User)
	$UserList .= $User."<br>";
echo "Users: ".count($Users)."<br>".$UserList;
?>