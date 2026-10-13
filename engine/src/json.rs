//! Minimal JSON reader/writer (no external crates).

#[derive(Clone, Debug)]
pub enum Json {
    Null,
    Bool(bool),
    Num(f64),
    Str(String),
    Arr(Vec<Json>),
    Obj(Vec<(String, Json)>),
}

impl Json {
    pub fn get(&self, k: &str) -> Option<&Json> {
        match self {
            Json::Obj(v) => v.iter().find(|(kk, _)| kk == k).map(|(_, v)| v),
            _ => None,
        }
    }
    pub fn num(&self) -> Option<f64> {
        match self {
            Json::Num(x) => Some(*x),
            _ => None,
        }
    }
    pub fn str(&self) -> Option<&str> {
        match self {
            Json::Str(s) => Some(s),
            _ => None,
        }
    }
    pub fn arr(&self) -> Option<&Vec<Json>> {
        match self {
            Json::Arr(a) => Some(a),
            _ => None,
        }
    }

    pub fn parse(s: &str) -> Result<Json, String> {
        let b = s.as_bytes();
        let mut i = 0;
        let v = parse_val(b, &mut i)?;
        skip_ws(b, &mut i);
        if i != b.len() {
            return Err(format!("trailing characters at {i}"));
        }
        Ok(v)
    }

    pub fn write(&self, out: &mut String) {
        match self {
            Json::Null => out.push_str("null"),
            Json::Bool(b) => out.push_str(if *b { "true" } else { "false" }),
            Json::Num(x) => {
                if x.is_finite() {
                    if (x - x.round()).abs() < 1e-12 && x.abs() < 1e15 {
                        out.push_str(&format!("{}", *x as i64));
                    } else {
                        out.push_str(&format!("{:.6}", x));
                    }
                } else {
                    out.push_str("null");
                }
            }
            Json::Str(s) => write_str(s, out),
            Json::Arr(a) => {
                out.push('[');
                for (i, v) in a.iter().enumerate() {
                    if i > 0 {
                        out.push(',');
                    }
                    v.write(out);
                }
                out.push(']');
            }
            Json::Obj(o) => {
                out.push('{');
                for (i, (k, v)) in o.iter().enumerate() {
                    if i > 0 {
                        out.push(',');
                    }
                    write_str(k, out);
                    out.push(':');
                    v.write(out);
                }
                out.push('}');
            }
        }
    }

    pub fn to_string(&self) -> String {
        let mut s = String::new();
        self.write(&mut s);
        s
    }
}

fn write_str(s: &str, out: &mut String) {
    out.push('"');
    for c in s.chars() {
        match c {
            '"' => out.push_str("\\\""),
            '\\' => out.push_str("\\\\"),
            '\n' => out.push_str("\\n"),
            c if (c as u32) < 0x20 => out.push_str(&format!("\\u{:04x}", c as u32)),
            c => out.push(c),
        }
    }
    out.push('"');
}

fn skip_ws(b: &[u8], i: &mut usize) {
    while *i < b.len() && (b[*i] as char).is_whitespace() {
        *i += 1;
    }
}

fn parse_val(b: &[u8], i: &mut usize) -> Result<Json, String> {
    skip_ws(b, i);
    if *i >= b.len() {
        return Err("unexpected end of JSON".into());
    }
    match b[*i] {
        b'{' => {
            *i += 1;
            let mut o = Vec::new();
            loop {
                skip_ws(b, i);
                if *i < b.len() && b[*i] == b'}' {
                    *i += 1;
                    return Ok(Json::Obj(o));
                }
                let k = match parse_val(b, i)? {
                    Json::Str(s) => s,
                    _ => return Err(format!("expected key at {i}")),
                };
                skip_ws(b, i);
                if *i >= b.len() || b[*i] != b':' {
                    return Err(format!("expected ':' at {i}"));
                }
                *i += 1;
                let v = parse_val(b, i)?;
                o.push((k, v));
                skip_ws(b, i);
                if *i < b.len() && b[*i] == b',' {
                    *i += 1;
                }
            }
        }
        b'[' => {
            *i += 1;
            let mut a = Vec::new();
            loop {
                skip_ws(b, i);
                if *i < b.len() && b[*i] == b']' {
                    *i += 1;
                    return Ok(Json::Arr(a));
                }
                a.push(parse_val(b, i)?);
                skip_ws(b, i);
                if *i < b.len() && b[*i] == b',' {
                    *i += 1;
                }
            }
        }
        b'"' => {
            *i += 1;
            let mut s = String::new();
            while *i < b.len() && b[*i] != b'"' {
                if b[*i] == b'\\' && *i + 1 < b.len() {
                    *i += 1;
                    match b[*i] {
                        b'n' => s.push('\n'),
                        b't' => s.push('\t'),
                        c => s.push(c as char),
                    }
                    *i += 1;
                } else {
                    // copy one UTF-8 char
                    let start = *i;
                    *i += 1;
                    while *i < b.len() && (b[*i] & 0xC0) == 0x80 {
                        *i += 1;
                    }
                    s.push_str(std::str::from_utf8(&b[start..*i]).map_err(|e| e.to_string())?);
                }
            }
            *i += 1;
            Ok(Json::Str(s))
        }
        b't' if b[*i..].starts_with(b"true") => {
            *i += 4;
            Ok(Json::Bool(true))
        }
        b'f' if b[*i..].starts_with(b"false") => {
            *i += 5;
            Ok(Json::Bool(false))
        }
        b'n' if b[*i..].starts_with(b"null") => {
            *i += 4;
            Ok(Json::Null)
        }
        _ => {
            let start = *i;
            while *i < b.len() && (b[*i] == b'-' || b[*i] == b'+' || b[*i] == b'.' || b[*i] == b'e' || b[*i] == b'E' || b[*i].is_ascii_digit()) {
                *i += 1;
            }
            let t = std::str::from_utf8(&b[start..*i]).unwrap();
            t.parse::<f64>().map(Json::Num).map_err(|_| format!("bad value at {start}"))
        }
    }
}
