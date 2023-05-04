<?php 
header("Content-Type: application/json; charset=UTF-8");

if($_GET['Action'] == 'SetOrderState'){
   
    $orderid = $_POST['OrderId'];
    $newstateid = $_POST['NewStateId'];

    $vals = "";

    foreach ($_GET as $key => $value) {
        $vals .=  "\n Get:" . $key . ':' . $value ; 
    }
    foreach ($_POST as $key => $value) {
        $vals .=  "\n Post:" . $key . ':' . $value ; 
    }

    $vals .= "\n";    
   

    $file = 'log.txt';
    // Öffnet die Datei, um den vorhandenen Inhalt zu laden
    $current = file_get_contents($file);
    // Fügt eine neue Person zur Datei hinzu
    $current .= "[Start]\n" . $orderid . " - " . $newstateid . $vals . "\n[Ende]\n";
    // Schreibt den Inhalt in die Datei zurück
    
    //file_put_contents($file, $current);
    file_put_contents($file, $current);
    $log_result = "";
    if($newstateid == "Versendet" || $newstateid == "shipped" || (int)$newstateid == 4){

        $data = array(
            "orderId" => (int)$orderid,
        );
    
        $result_confirm = CallAPI("POST","https://www.fruugo.com/orders/confirm", $data);
        
        $result = CallAPI("POST","https://www.fruugo.com/orders/ship", $data);

        $log_result = $result . " " . $result_confirm;
    }
    $file = 'log.txt';
    // Öffnet die Datei, um den vorhandenen Inhalt zu laden
    $current = file_get_contents($file);
    // Fügt eine neue Person zur Datei hinzu
    $current .= "[Start]\n" . $orderid . " - " . $newstateid . $vals . "\n[Ende]\n";
    // Schreibt den Inhalt in die Datei zurück
    
    //file_put_contents($file, $current);
    file_put_contents($file, $current);
    //Versuchen alle alten Orders aus versand zusetzen, shipping = rechnungsdatum ab 20.03
}

if($_GET['Action'] == 'GetOrders'){
    $date = $_GET['StartDate'];
    $data = array(
        "from" => date('Y-m-d', strtotime($date)),
    );

    

    if(isset($_GET['Page']))
        $page = $_GET['Page'];
    else    
        $page = 1;
    
    if(isset($_GET['PageSize']))
        $pagesize = $_GET['PageSize'];
    else    
        $pagesize = 0;    
    
    $xml_orders = CallAPI("GET","https://www.fruugo.com/orders/download", $data);
//var_dump($xml_orders);
    $orders = new SimpleXMLElement($xml_orders);
    
    $orders_obj = $orders->children("https://www.fruugo.com/orders/schema");
    
    $orders = array();
    
    $counter = 0;
        
       
    $min = (((int)$pagesize * (int)$page) - (int)$pagesize);
    $max = (int)$pagesize * (int)$page;
    $min++; 

    foreach($orders_obj as $order_obj){
        //Anzahl und Seite auswählen
        $counter++;

        if($pagesize > 0){
            if($counter > $max){
              break;
            } 
            elseif($counter < $min){    
                continue;        
            }    
        }    

        //produkte in array schreiben
        $produkte = array();
       
        foreach($order_obj->orderLines->orderLine as $produkt){
            $artikel = explode(" - ", $produkt->skuName);

            $produkte[] = array(
                            "product_id"    => (string)$produkt->productId,
                            "name"          => $artikel[3] . " - " . $artikel[4] . " - " . $artikel[5] . " - " . $artikel[6] . " - " . $artikel[7], 
                            "sku"           => (string)$produkt->skuId,
                            "quantity"      => floatval($produkt->totalNumberOfItems),
                            "unit_price"    => floatval($produkt->itemPriceInclVat), 
                            "tax_rate"      => floatval($produkt->vatPercentage),
                            "options"       => array(

                            ), 
            );
        }

        $orders[] = array(
                    "order_id"          => (string)$order_obj->orderId[0],
                    "order_number"      => (string)$order_obj->orderId[0],
                    "customer_id"       => (string)$order_obj->customerOrderId[0],
                    "email"             => "",
                    "phone1"            => (string)$order_obj->phoneNumber[0],
                    "phone2"            => null,
                    "fax"               => "",
                    "vat_id"            => "",
                    "vat_mode"          => null,
                    "invoice_address"   => array(
                                            "firstname"     => (string)$order_obj->shippingAddress->firstName[0],    
                                            "name2"         => null,    
                                            "lastname"      => (string)$order_obj->shippingAddress->lastName[0],    
                                            "company"       => null,
                                            "street"        => (string)$order_obj->shippingAddress->streetAddress[0],
                                            "housenumber"   => null,
                                            "address2"      => null,
                                            "city"          => (string)$order_obj->shippingAddress->city[0],
                                            "postcode"      => (string)$order_obj->shippingAddress->postalCode[0],
                                            "country_code"  => (string)$order_obj->shippingAddress->countryCode[0],
                                            "statte"        => null,
                    ),        
                    "delivery_address"   => array(
                                            "firstname"     => (string)$order_obj->shippingAddress->firstName[0],    
                                            "name2"         => null,    
                                            "lastname"      => (string)$order_obj->shippingAddress->lastName[0],    
                                            "company"       => null,
                                            "street"        => (string)$order_obj->shippingAddress->streetAddress[0],
                                            "housenumber"   => null,
                                            "address2"      => null,
                                            "city"          => (string)$order_obj->shippingAddress->city[0],
                                            "postcode"      => (string)$order_obj->shippingAddress->postalCode[0],
                                            "country_code"  => (string)$order_obj->shippingAddress->countryCode[0],
                                            "statte"        => null,
                    ),
                    "payment_method"    =>  94, // 94 Fruugo -  93 Real - 95 CDISCOUNT
                    "order_status_id"   =>  3, // Bezahlt
                    "currency_code"     =>  "EUR",       
                    "order_date"        =>  (string)$order_obj->orderDate[0],       
                    "pay_date"          =>  (string)$order_obj->orderReleaseDate[0],
                    "ship_date"         =>  null,
                    "ship_cost"         =>  floatval($order_obj->shippingCostInclVAT[0]),
                    "order_products"    =>  $produkte,
                    "order_history"     =>  array(),
        );
    }
    
    $total_page = 1; 
    if($pagesize > 0)
        $total_page = ceil(sizeof($orders_obj) / $pagesize);

    $paging = array(
        'page' => $page,
        'totalCount' => sizeof($orders_obj),
        'totalPages' => ceil(sizeof($orders_obj) / $total_page),
    );

    $json = array(
        'paging' => $paging,
        'orders' => $orders, 
    );

    echo(json_encode($json));
}

function getSignature($apikey){
    $unixtimestamp = substr(time(), 0, 7); 
    // API Passwort, kann beliebig festgelegt werden 
    $pwd = $apikey;  
    //strings werden UTF8 kodiert  
    // API Passwort wird mit Algorithmus SHA256 und dem Key Timestamp verschlüsselt 
    $hash = hash_hmac( "sha256", utf8_encode($pwd), utf8_encode($unixtimestamp)); 
    // Das Ergebnis wird BASE64 kodiert 
    $bsec = base64_encode($hash); // HTML spezifische Zeichen werden entfernt 
    $bsec = str_replace("=","",$bsec);
    $bsec = str_replace("/","",$bsec); 
    $bsec = str_replace("+","",$bsec); 
 
    return $bsec; 
}

function CallAPI($method, $url, $data = false)
{
    $curl = curl_init();

    switch ($method)
    {
        case "POST":
            curl_setopt($curl, CURLOPT_POST, 1);

            if ($data){
                foreach($data as $key=>$value) {
                    $postvars .= $key . "=" . $value . "&";
                  }      
                curl_setopt($curl, CURLOPT_POSTFIELDS, $postvars);
            }
            break;
        case "PUT":
            curl_setopt($curl, CURLOPT_PUT, 1);
            break;
        default:
            if ($data)
                $url = sprintf("%s?%s", $url, http_build_query($data));
    }

    // Optional Authentication:
    curl_setopt($curl, CURLOPT_HTTPAUTH, CURLAUTH_BASIC);
    //curl_setopt($curl, CURLOPT_USERPWD, "frank.laue@saflax.de:UkCpk2wt");
    curl_setopt($curl, CURLOPT_USERPWD, "frank.laue@saflax.de:48163Gropius#");
    
    curl_setopt($curl, CURLOPT_URL, $url);
    curl_setopt($curl, CURLOPT_RETURNTRANSFER, 1);

    $result = curl_exec($curl);

    curl_close($curl);

    return $result;
}

?>