function ready(fn) {
    if (document.readyState === "complete" || document.readyState === "interactive") {
        setTimeout(fn, 1);
    } else {
        document.addEventListener("DOMContentLoaded", fn);
    }
}  

ready(function() {
    var ready = document.getElementById("ready");
    var site = document.getElementById("site");
    var prepare = document.getElementById("prepare");
    var done = document.getElementById("done");
    var copyIds = document.getElementById("copy-ids");
    var copyNames = document.getElementById("copy-names");
    var ids = document.getElementById("ids");
    var names = document.getElementById("names");

    ready.innerHTML = "Ready!";

    prepare.addEventListener("click", (e) => {
        switch(site.value) {
            case "search":
                prepareSearch();
            case "osu":
                prepareOsu();
        }

        done.innerHTML = "Done!";
    });


    function prepareSearch() {
        document.querySelectorAll(".ten.wide.column.beatmap-info").forEach(el => {
            var artist = el.children[1].querySelector(":scope span").innerHTML;
            var title = el.querySelector(":scope .beatmap-title a").innerHTML;
        
            names.insertAdjacentHTML("afterbegin", artist + " - " + title + "\n");
        })
    }

    function prepareOsu() {
        document.querySelectorAll(".beatmap").forEach(el => {
            var artist = el.querySelector(":scope .artist").innerHTML;
            var title = el.querySelector(":scope .title").innerHTML;
        
            ids.insertAdjacentHTML("afterbegin", el.id + "\n");
            names.insertAdjacentHTML("afterbegin", artist + " - " + title + "\n");
        })
    }
    
    copyIds.addEventListener("click", (e) => {
        ids.select();
        document.execCommand("copy");
    });

    copyNames.addEventListener("click", (e) => {
        names.select();
        document.execCommand("copy");
    });
})